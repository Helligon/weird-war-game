using System;
using System.Collections.Generic;
using System.Linq;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Commands
{
    /// <summary>
    /// Moves one of the issuer's Brigades along a path of adjacent hexes (TRN-002, SUP-005, RES-004). The path lists
    /// the hexes entered, in order, not the start hex. Validation covers the whole path, so a rejected move changes
    /// nothing. Apply then takes the SP hex by hex (TRN-029) and counts the hexes moved this turn (SUP-024).
    ///
    /// Rules: SP is the limit (SUP-005); the SP comes only from the Brigade, never the reserve (SUP-015), and the Brigade stops on the last
    /// hex it can pay for (SUP-005, SUP-006; a move that cannot pay for the first hex is rejected). A Brigade with a Slow Section
    /// may move at most P-SUP-10 hexes per turn in total (TAG-010, SUP-024, SUP-025). A hex that is impassable terrain,
    /// off the map or held by an enemy cannot be entered (TRN-023, TRN-024). No Brigade may end its move on an occupied
    /// hex, friendly or enemy (TRN-020); a path may pass through a friendly Brigade (TRN-023 lists only enemies as blocking).
    /// Entering a hex next to an enemy Brigade is contact: the move stops there and a battle starts (TRN-003, TRN-008); a one-hex order at an adjacent enemy hex is an attack that walks nothing and costs P-TRN-02 SP (TRN-036).
    /// A move may be split over the Actions phase (TRN-030) and a Brigade can move the turn it appears (TRN-032).
    /// </summary>
    public sealed class MoveBrigadeCommand : ICommand, IEquatable<MoveBrigadeCommand>
    {
        public const string TypeName = "moveBrigade";

        private readonly HexCoord[] _path;

        public MoveBrigadeCommand(string? faction, int brigadeId, IEnumerable<HexCoord> path)
        {
            Issuer = faction ?? "";
            BrigadeId = brigadeId;
            _path = path == null ? new HexCoord[0] : path.ToArray(); // null becomes an empty path (Validate rejects it): the reflection test GameStateTests.TEC_008_every_command_type... builds every command from null arguments
        }

        public int BrigadeId { get; }

        /// <summary>The hexes entered, in order.</summary>
        public IReadOnlyList<HexCoord> Path => _path;

        public string Type => TypeName;
        public string Issuer { get; }
        public TurnPhase? RequiredPhase => TurnPhase.Actions;

        /// <summary>One hex entered and what entering it costs.</summary>
        private readonly struct Step
        {
            public Step(HexCoord hex, int cost) { Hex = hex; Cost = cost; }
            public HexCoord Hex { get; }
            public int Cost { get; }
        }

        /// <summary>What a walk found: the steps to take, and the battle (if any) that starts.</summary>
        private sealed class Walked
        {
            public Brigade? Brigade;
            public List<Step> Steps = new List<Step>();
            /// <summary>Hexes that count towards the Slow cap: the whole ordered path, or 0 for an attack (answer 2, TRN-036).</summary>
            public int PathLength;
            /// <summary>The enemy Brigade the battle is against, or null for no battle (TRN-003, TRN-036).</summary>
            public Brigade? Foe;
            /// <summary>SP paid for an attack from an adjacent hex (TRN-036, P-TRN-02); 0 otherwise.</summary>
            public int AttackCost;
        }

        /// <summary>
        /// The single walk along the path, used by both Validate and Apply so they cannot drift apart. It stops at contact
        /// (TRN-003, TRN-008): the hexes after the contact hex are not walked and not checked. It reads the state and never
        /// changes it. Returns a reason the move is rejected, or null.
        /// </summary>
        private string? Walk(GameState state, Walked w)
        {
            w.PathLength = _path.Length;
            if (state.Map == null || state.Supply == null || state.MovementRules == null) return "There is no campaign to move in";
            var mover = state.Brigades.FirstOrDefault(b => b.Id == BrigadeId);
            if (mover == null) return "There is no Brigade " + BrigadeId;
            w.Brigade = mover;
            if (mover.Faction != Issuer) return "Brigade " + BrigadeId + " belongs to " + mover.Faction + ", not " + Issuer;
            if (_path.Length == 0) return "A move must enter at least one hex";
            // CC1 assumption (DEC-103: what follows "battle started" is out of scope): a Brigade in a pending battle, on either
            // side, does not move or attack again. TRN-015 says nothing moves the attacker afterwards.
            if (state.Battles.Any(x => x.AttackerId == mover.Id || x.DefenderId == mover.Id))
                return "Brigade " + mover.Id + " is in a battle that has started and cannot move or attack (TRN-015)";

            var map = state.Map;
            var perHex = state.Supply.CostPerHexOf(mover);
            var from = mover.Position;
            var contactAt = 0;
            for (var i = 0; i < _path.Length; i++)
            {
                var hex = _path[i];
                if (from.DistanceTo(hex) != 1) return "Hex " + hex + " is not next to " + from;
                if (!map.Contains(hex)) return "Hex " + hex + " is beyond the map edge (TRN-023)";
                var factor = state.TerrainCostFactor(hex);
                if (factor == null) return "Hex " + hex + " is impassable " + map.TerrainAt(hex).ToString().ToLowerInvariant() + " (TRN-023, TRN-024)";
                var holder = state.Brigades.FirstOrDefault(b => b.Id != mover.Id && b.Position == hex);
                if (holder != null && holder.Faction != mover.Faction)
                {
                    // TRN-036: a one-hex order at an adjacent enemy hex is an attack. Anywhere else an enemy hex blocks (TRN-023).
                    if (_path.Length != 1) return "Hex " + hex + " is held by an enemy Brigade (TRN-023)";
                    var cost = state.MovementRules.AttackCost;
                    if (mover.SP < cost) return "Attacking costs " + cost + " SP (P-TRN-02) and the Brigade has " + mover.SP + " (TRN-036)";
                    w.Foe = holder;
                    w.AttackCost = cost;
                    w.PathLength = 0; // an attack walks 0 hexes
                    return null;
                }
                // TRN-020: passing through a friendly Brigade is allowed (TRN-023 lists only enemy Brigades as blocking).
                w.Steps.Add(new Step(hex, perHex * factor.Value));
                from = hex;
                // TRN-003: contact is entering a hex next to an enemy Brigade. TRN-008: the move stops there.
                if (w.Foe == null)
                {
                    w.Foe = state.Brigades.Where(b => b.Faction != mover.Faction && b.Position.DistanceTo(hex) == 1).OrderBy(b => b.Id).FirstOrDefault();
                    if (w.Foe != null) contactAt = w.Steps.Count;
                }
            }

            // TRN-023 checked the whole ordered path above; TRN-008 only stops it being walked past the contact hex.
            if (w.Foe != null) w.Steps.RemoveRange(contactAt, w.Steps.Count - contactAt);

            // SUP-005, SUP-006: the Brigade stops on the last hex its own SP pays for (SUP-015: never the reserve).
            // The path up to contact is checked above; SP may stop it earlier, and then contact did not happen.
            var steps = w.Steps;
            long left = mover.SP;
            var paid = 0;
            while (paid < steps.Count && steps[paid].Cost <= left) left -= steps[paid++].Cost;
            if (paid == 0) return "The Brigade has " + mover.SP + " SP and cannot pay " + steps[0].Cost + " for the first hex; SP may not go below 0 (SUP-006, SUP-015)";
            if (paid < steps.Count) w.Foe = null;
            steps.RemoveRange(paid, steps.Count - paid);

            // TRN-020, TRN-008: no Brigade may end its move on an occupied hex, so a move whose contact (or SP) stop is a
            // friendly-held hex is rejected. The hex it stops on is the one that counts.
            var end = steps[steps.Count - 1].Hex;
            var onEnd = state.Brigades.FirstOrDefault(b => b.Id != mover.Id && b.Position == end);
            if (onEnd != null) return "Hex " + end + " is held by Brigade " + onEnd.Id + "; two Brigades cannot share a hex (TRN-020, TRN-008)";
            return null;
        }

        public string? Validate(GameState state)
        {
            var w = new Walked();
            var reason = Walk(state, w);
            if (reason != null) return reason;
            var b = w.Brigade!;

            if (b.Sections.Any(s => state.SectionTypes!.Get(s.TypeId).Slow))
            {
                var cap = state.MovementRules!.SlowCap;
                var moved = b.HexesMovedIn(state.TurnNumber);
                if (moved + w.PathLength > cap)
                    return "A Slow Brigade may move " + cap + " hexes a turn (P-SUP-10); it has moved " + moved + " and this move adds " + w.PathLength;
            }

            return null;
        }

        public void Apply(GameState state)
        {
            var w = new Walked();
            Walk(state, w);
            var b = w.Brigade!;
            // A new turn starts the count afresh (SUP-024).
            if (b.MovedTurn != state.TurnNumber) { b.MovedTurn = state.TurnNumber; b.MovedHexes = 0; }
            foreach (var step in w.Steps)
            {
                // TRN-029: SP is taken as each hex is entered.
                b.SP -= step.Cost;
                b.Position = step.Hex;
                b.MovedHexes++;
            }
            // TRN-036: an attack from an adjacent hex costs P-TRN-02 and walks nothing.
            b.SP -= w.AttackCost;
            // TRN-003, TRN-008: "battle started", both Brigades on their own hexes, no outcome.
            if (w.Foe != null) state.StartBattle(b.Id, w.Foe.Id);
        }

        public Dictionary<string, object?> ToJson()
        {
            var path = new List<object?>();
            foreach (var hex in _path)
            {
                var (c, r) = hex.ToOffset();
                path.Add(new List<object?> { (long)c, (long)r });
            }
            return CommandRegistry.WithIssuer(new Dictionary<string, object?> { ["brigade"] = (long)BrigadeId, ["path"] = path }, Issuer);
        }

        public static MoveBrigadeCommand FromJson(IReadOnlyDictionary<string, object?> json)
        {
            var id = CommandRegistry.RequireLong(json, "brigade");
            if (id < int.MinValue || id > int.MaxValue) throw new JsonException("Field 'brigade' out of range");
            if (!json.TryGetValue("path", out var raw) || !(raw is List<object?> list)) throw new JsonException("Field 'path' must be an array of [column,row] pairs");
            var path = new List<HexCoord>();
            foreach (var item in list)
            {
                if (!(item is List<object?> pair) || pair.Count != 2 || !(pair[0] is long c) || !(pair[1] is long r)
                    || c < 0 || c > 100000 || r < 0 || r > 100000)
                    throw new JsonException("Each path entry must be a [column,row] pair of whole numbers");
                path.Add(HexCoord.FromOffset((int)c, (int)r));
            }
            return new MoveBrigadeCommand(CommandRegistry.OptionalIssuer(json), (int)id, path);
        }

        public bool Equals(MoveBrigadeCommand? other) =>
            other != null && other.Issuer == Issuer && other.BrigadeId == BrigadeId && other._path.SequenceEqual(_path);
        public override bool Equals(object? obj) => Equals(obj as MoveBrigadeCommand);
        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(Issuer);
            h.Add(BrigadeId);
            foreach (var hex in _path) h.Add(hex);
            return h.ToHashCode();
        }
    }
}
