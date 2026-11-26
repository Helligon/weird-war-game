using System;
using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// The player's tactical view of one battle (CMD-001, CMD-002, CMD-004, CMD-016, HIE-007): the mode (in or out), whether the
    /// battle is running, which one Section is selected, and the waypoints being planned. Plain C# with no Unity reference
    /// (DLV-012), so the rules of the screen are tested without Unity.
    /// It holds UI state only. Every order it issues is a command applied with <see cref="BattleState.ApplyCommand"/> (DLV-013),
    /// and the mode flags never create a command: pausing only means that whoever owns the clock stops calling
    /// <see cref="BattleState.Step"/> while <see cref="Running"/> is false (CMD-002).
    /// </summary>
    public enum TacticalAction { ConfirmMove, CancelPending, Stop, ClearWaypoints, FallBack }

    public sealed class TacticalView
    {
        /// <summary>The panel buttons, in order. The panel is built from <see cref="Buttons"/> only, so no button can exist that is not listed (CMD-013).</summary>
        public static readonly IReadOnlyList<KeyValuePair<TacticalAction, string>> Buttons = new[]
        {
            new KeyValuePair<TacticalAction, string>(TacticalAction.ConfirmMove, "Confirm Move"),
            new KeyValuePair<TacticalAction, string>(TacticalAction.CancelPending, "Cancel planned waypoints"),
            new KeyValuePair<TacticalAction, string>(TacticalAction.Stop, "Stop (hold)"),
            new KeyValuePair<TacticalAction, string>(TacticalAction.ClearWaypoints, "Clear waypoints"),
            new KeyValuePair<TacticalAction, string>(TacticalAction.FallBack, "Fall back"),
        };

        /// <summary>
        /// Every control the tactical view offers: the clicks (select a friendly Section, place a waypoint, Target an enemy Section)
        /// and the <see cref="Buttons"/>. CMD-013: there is deliberately no Section ability trigger.
        /// </summary>
        public static IReadOnlyList<string> Controls
        {
            get
            {
                var list = new List<string> { "Select Section", "Place waypoint", "Target" };
                foreach (var b in Buttons) list.Add(b.Value);
                return list;
            }
        }

        /// <summary>Does what a panel button does.</summary>
        public bool Perform(TacticalAction action)
        {
            switch (action)
            {
                case TacticalAction.ConfirmMove: return ConfirmMove();
                case TacticalAction.CancelPending:
                    if (!Active) return Fail("Section commands are only possible in the tactical view (CMD-001).");
                    CancelPending();
                    return true;
                case TacticalAction.Stop: return Stop();
                case TacticalAction.ClearWaypoints: return ClearWaypoints();
                case TacticalAction.FallBack: return FallBack();
                default: return false;
            }
        }

        /// <summary>Metres from a Section's centre within which a click picks that Section (a stand-in sized to the drawn Unit grid).</summary>
        public const double PickRadius = 5.0;

        /// <summary>The index of the Officer the player controls (stand-in: one Officer in the spike).</summary>
        public const int ControlledOfficer = 0;

        private readonly BattleState _state;
        private readonly List<Waypoint> _pending = new List<Waypoint>();
        private SectionId? _selected;

        public TacticalView(BattleState state) { _state = state ?? throw new ArgumentNullException(nameof(state)); }

        /// <summary>True while the player is in the tactical view.</summary>
        public bool Active { get; private set; }

        /// <summary>
        /// True while the battle is running. Whoever owns the clock calls <see cref="BattleState.Step"/> only while this is true
        /// (movement, fire and AI all stop when it is false, CMD-002). The battle starts paused.
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>CMD-002: entering the tactical view pauses the battle.</summary>
        public void Enter() { Active = true; Running = false; SyncOfficer(); }

        /// <summary>
        /// CMD-002: leaving the tactical view resumes the battle. Unfinished waypoints are dropped. CMD-004: the player controls the
        /// Officer again. PCH-006: refused (and false) when the player has no living Officer: the tactical view is then the only view.
        /// </summary>
        public bool Exit()
        {
            if (!CanReturnToThirdPerson) return Fail("The Officer is dead and there is no other officer or hero: tactical view only (PCH-006).");
            Active = false; Running = true; _pending.Clear();
            SyncOfficer();
            return true;
        }

        /// <summary>Run or pause the battle (the Run/Pause button). It is allowed inside the tactical view too (CMD-004).</summary>
        public void SetRunning(bool running) { Running = running; SyncOfficer(); }

        /// <summary>PCH-006: true while the player has a living Officer to go back to (a stand-in for "an officer or hero").</summary>
        public bool CanReturnToThirdPerson => _state.HasLivingOfficer(Side.Player);

        /// <summary>
        /// PCH-006: call after the battle steps. When the player's last Officer has died while the player is in third person, the game
        /// switches to the tactical view with the battle paused (CMD-002) and this returns true. Otherwise it does nothing.
        /// </summary>
        public bool CheckOfficerLost()
        {
            if (Active || CanReturnToThirdPerson) return false;
            Enter();
            Message = "Your Officer has fallen. Tactical view only (PCH-006).";
            return true;
        }

        /// <summary>
        /// CMD-004, DAI-005: makes the core's Officer AI state match <see cref="OfficerUnderAi"/>. The change is an
        /// <see cref="OfficerControlCommand"/> applied through the core (DLV-013), so it is logged and replays (TEC-008).
        /// The AI itself runs inside the core's step and never reads this view.
        /// </summary>
        private void SyncOfficer()
        {
            var wantAi = OfficerUnderAi;
            // Only the Officer the player controls is handed over. Spike stand-in: that is Officer 0 (PCH-003 is not built yet).
            if (_state.Officers.Count == 0) return;
            var o = _state.Officers[ControlledOfficer];
            if (o.Side != Side.Player || o.IsDestroyed || o.UnderAi == wantAi) return;
            _state.ApplyCommand(new OfficerControlCommand(ControlledOfficer, wantAi));
        }

        /// <summary>
        /// CMD-004: true when the battle runs while the player is still in the tactical view. The Officer is then under AI control
        /// and takes no player input. The AI itself (DAI-005) is in the core; this flag only decides when the view hands the Officer
        /// over, by sending an <see cref="OfficerControlCommand"/>.
        /// </summary>
        public bool OfficerUnderAi => Active && Running;

        /// <summary>True when the Officer may be given player input: never while the player is in the tactical view (CMD-004).</summary>
        public bool OfficerAcceptsInput => !Active;

        /// <summary>CMD-001: Section commands are only possible inside the tactical view.</summary>
        public bool CanCommandSections => Active;

        /// <summary>What the last action did, in words, for the screen.</summary>
        public string Message { get; private set; } = "";

        /// <summary>The selected friendly Section, or null (CMD-016: at most one). A destroyed Section is no longer selected.</summary>
        public SectionId? Selected
        {
            get
            {
                if (_selected.HasValue)
                {
                    var s = _state.Find(_selected.Value);
                    if (s == null || s.IsDestroyed) { _selected = null; _pending.Clear(); }
                }
                return _selected;
            }
        }

        /// <summary>Waypoints placed but not yet confirmed. They are not commands until <see cref="ConfirmMove"/>.</summary>
        public IReadOnlyList<Waypoint> PendingWaypoints => _pending;

        /// <summary>How many more waypoints may be placed on the selected Section: P-CMD-01 minus queued minus pending (CMD-020).</summary>
        public int WaypointsLeft
        {
            get
            {
                var id = Selected;
                if (!id.HasValue) return 0;
                var queued = _state.Find(id.Value)!.Waypoints.Count;
                return Math.Max(0, _state.Parameters.MaxWaypoints - queued - _pending.Count);
            }
        }

        /// <summary>
        /// HIE-007: the living Section on the given side whose centre is nearest to the point and within <see cref="PickRadius"/>,
        /// or null. A click always picks a Section, never a Unit.
        /// </summary>
        public SectionId? PickSection(MapPoint at, Side side)
        {
            SectionId? best = null;
            var bestDistance = PickRadius;
            foreach (var s in _state.Sections)
            {
                if (s.Side != side || s.IsDestroyed) continue;
                var dx = s.Position.X - at.X;
                var dy = s.Position.Y - at.Y;
                var d = Math.Sqrt(dx * dx + dy * dy);
                if (d <= bestDistance) { best = s.Id; bestDistance = d; }
            }
            return best;
        }

        /// <summary>CMD-016: selects one friendly Section, replacing any earlier selection. Unplaced waypoints are dropped.</summary>
        public bool Select(SectionId id)
        {
            if (!Active) return Fail("Section commands are only possible in the tactical view (CMD-001).");
            var s = _state.Find(id);
            if (s == null || s.Side != Side.Player || s.IsDestroyed) return Fail("Only a living friendly Section can be selected.");
            if (!_selected.Equals(id)) _pending.Clear();
            _selected = id;
            Message = id + " selected.";
            return true;
        }

        public void Deselect() { _selected = null; _pending.Clear(); Message = "Nothing selected."; }

        /// <summary>
        /// CMD-005, CMD-020: adds a waypoint (whole metres) to the plan. Refused when no Section is selected, when the point is off the
        /// map, or when queued plus pending waypoints would pass P-CMD-01. The core checks the same cap again when the Move is applied.
        /// </summary>
        public bool AddWaypoint(MapPoint at)
        {
            if (!Active) return Fail("Section commands are only possible in the tactical view (CMD-001).");
            if (!Selected.HasValue) return Fail("Select a Section first.");
            var q = (int)Math.Round(at.X);
            var r = (int)Math.Round(at.Y);
            if (q < 0 || q > _state.Map.Width || r < 0 || r > _state.Map.Height) return Fail("That point is off the map.");
            if (WaypointsLeft < 1) return Fail("No more waypoints: the limit is " + _state.Parameters.MaxWaypoints + " including queued ones (P-CMD-01).");
            _pending.Add(new Waypoint(q, r));
            Message = "Waypoint " + _pending.Count + " placed at " + q + ", " + r + ".";
            return true;
        }

        /// <summary>Drops the waypoints placed so far. It issues no command (use <see cref="ClearWaypoints"/> for queued ones).</summary>
        public void CancelPending() { _pending.Clear(); Message = "Planned waypoints cancelled."; }

        /// <summary>CMD-005: issues one Move with the planned waypoints.</summary>
        public bool ConfirmMove()
        {
            if (!Active) return Fail("Section commands are only possible in the tactical view (CMD-001).");
            if (_pending.Count == 0) return Fail("Place at least one waypoint first.");
            var id = Selected;
            if (!id.HasValue) return Fail("Select a Section first.");
            var ok = Issue(new MoveCommand(id.Value, _pending));
            if (ok) _pending.Clear();
            return ok;
        }

        /// <summary>CMD-006.</summary>
        public bool Stop() => IssueForSelected(id => new StopCommand(id));

        /// <summary>CMD-007: clears the queued waypoints (the planned, unconfirmed ones go too).</summary>
        public bool ClearWaypoints()
        {
            var ok = IssueForSelected(id => new ClearWaypointsCommand(id));
            if (ok) _pending.Clear();
            return ok;
        }

        /// <summary>CMD-011.</summary>
        public bool FallBack()
        {
            var ok = IssueForSelected(id => new FallBackCommand(id));
            if (ok) _pending.Clear();
            return ok;
        }

        /// <summary>CMD-008: the selected Section attacks the given enemy Section.</summary>
        public bool Target(SectionId enemy) => IssueForSelected(id => new TargetCommand(id, enemy));

        /// <summary>
        /// A click on the map at a point: a friendly Section there is selected (CMD-016); with a Section selected, an enemy Section
        /// there is targeted (CMD-008); anywhere else adds a waypoint (CMD-005). Returns true when something was done.
        /// </summary>
        public bool Click(MapPoint at)
        {
            if (!Active) return Fail("Section commands are only possible in the tactical view (CMD-001).");
            var friendly = PickSection(at, Side.Player);
            if (friendly.HasValue) return Select(friendly.Value);
            var enemy = PickSection(at, Side.Enemy);
            if (enemy.HasValue && Selected.HasValue) return Target(enemy.Value);
            return AddWaypoint(at);
        }

        private bool IssueForSelected(Func<SectionId, IBattleCommand> make)
        {
            if (!Active) return Fail("Section commands are only possible in the tactical view (CMD-001).");
            var id = Selected;
            if (!id.HasValue) return Fail("Select a Section first.");
            return Issue(make(id.Value));
        }

        private bool Issue(IBattleCommand command)
        {
            var result = _state.ApplyCommand(command);
            Message = result.IsAccepted ? command.GetType().Name.Replace("Command", "") + " ordered (step " + _state.StepCount + ")." : "Refused: " + result.Reason;
            return result.IsAccepted;
        }

        private bool Fail(string message) { Message = message; return false; }
    }
}
