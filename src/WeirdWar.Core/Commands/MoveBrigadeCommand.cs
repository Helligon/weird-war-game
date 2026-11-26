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
    /// Rules: SP is the limit (SUP-005); the SP comes only from the Brigade, never the reserve (SUP-015), and a move
    /// that would take it below 0 is rejected (SUP-006; the battle exception is CC1-73). A Brigade with a Slow Section
    /// may move at most P-SUP-10 hexes per turn in total (TAG-010, SUP-024, SUP-025). A hex that is impassable terrain,
    /// off the map or held by an enemy cannot be entered (TRN-023, TRN-024). No Brigade may end its move on an occupied
    /// hex, friendly or enemy (TRN-020); a path may pass through a friendly Brigade (TRN-023 lists only enemies as blocking).
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

        /// <summary>
        /// The single walk along the path, used by both Validate and Apply so they cannot drift apart (CC1-73 will stop
        /// it at contact here). Returns the steps, or a reason the path cannot be walked. It reads the state and never changes it.
        /// </summary>
        private string? Walk(GameState state, out Brigade? brigade, out List<Step> steps)
        {
            steps = new List<Step>();
            brigade = null;
            if (state.Map == null || state.Supply == null || state.MovementRules == null) return "There is no campaign to move in";
            var mover = state.Brigades.FirstOrDefault(b => b.Id == BrigadeId);
            if (mover == null) return "There is no Brigade " + BrigadeId;
            brigade = mover;
            if (mover.Faction != Issuer) return "Brigade " + BrigadeId + " belongs to " + mover.Faction + ", not " + Issuer;
            if (_path.Length == 0) return "A move must enter at least one hex";

            var map = state.Map;
            var perHex = state.Supply.CostPerHexOf(mover);
            var from = mover.Position;
            for (var i = 0; i < _path.Length; i++)
            {
                var hex = _path[i];
                if (from.DistanceTo(hex) != 1) return "Hex " + hex + " is not next to " + from;
                if (!map.Contains(hex)) return "Hex " + hex + " is beyond the map edge (TRN-023)";
                var factor = map.TerrainCostFactor(hex);
                if (factor == null) return "Hex " + hex + " is impassable " + map.TerrainAt(hex).ToString().ToLowerInvariant() + " (TRN-023, TRN-024)";
                var holder = state.Brigades.FirstOrDefault(b => b.Id != mover.Id && b.Position == hex);
                if (holder != null)
                {
                    // TRN-023: an enemy hex is impassable anywhere on the path. TRN-020: no Brigade may end on an occupied hex;
                    // passing through a friendly one is allowed (TRN-023 lists only enemy Brigades as blocking).
                    if (holder.Faction != mover.Faction) return "Hex " + hex + " is held by an enemy Brigade (TRN-023)";
                    if (i == _path.Length - 1) return "Hex " + hex + " is held by Brigade " + holder.Id + "; two Brigades cannot share a hex (TRN-020)";
                }
                steps.Add(new Step(hex, perHex * factor.Value));
                from = hex;
            }
            return null;
        }

        public string? Validate(GameState state)
        {
            var reason = Walk(state, out var brigade, out var steps);
            if (reason != null) return reason;
            var b = brigade!;

            if (b.Sections.Any(s => state.SectionTypes!.Get(s.TypeId).Slow))
            {
                var cap = state.MovementRules!.SlowCap;
                var moved = b.HexesMovedIn(state.TurnNumber);
                if (moved + steps.Count > cap)
                    return "A Slow Brigade may move " + cap + " hexes a turn (P-SUP-10); it has moved " + moved + " and this move adds " + steps.Count;
            }

            long cost = steps.Sum(s => (long)s.Cost);
            if (b.SP - cost < 0)
                return "Moving " + steps.Count + " hexes costs " + cost + " SP and the Brigade has " + b.SP + "; SP may not go below 0 (SUP-006, SUP-015)";
            return null;
        }

        public void Apply(GameState state)
        {
            Walk(state, out var brigade, out var steps);
            var b = brigade!;
            // A new turn starts the count afresh (SUP-024).
            if (b.MovedTurn != state.TurnNumber) { b.MovedTurn = state.TurnNumber; b.MovedHexes = 0; }
            foreach (var step in steps)
            {
                // TRN-029: SP is taken as each hex is entered.
                b.SP -= step.Cost;
                b.Position = step.Hex;
                b.MovedHexes++;
            }
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
