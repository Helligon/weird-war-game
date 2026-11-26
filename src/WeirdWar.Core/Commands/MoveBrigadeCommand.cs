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
    /// off the map or held by an enemy cannot be entered (TRN-023, TRN-024). A hex held by any other Brigade cannot be
    /// entered at all, so no two Brigades share a hex (TRN-020). That is stricter than the acceptance criterion, which
    /// only names the last hex, because the pass-through exception exists only for retreats (TRN-021).
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
            _path = path == null ? new HexCoord[0] : path.ToArray(); // no path is an empty one, which Validate rejects
        }

        public int BrigadeId { get; }

        /// <summary>The hexes entered, in order.</summary>
        public IReadOnlyList<HexCoord> Path => _path;

        public string Type => TypeName;
        public string Issuer { get; }
        public TurnPhase? RequiredPhase => TurnPhase.Actions;

        public string? Validate(GameState state)
        {
            if (state.Map == null || state.SectionTypes == null || state.Parameters == null) return "There is no campaign to move in";
            var brigade = state.Brigades.FirstOrDefault(b => b.Id == BrigadeId);
            if (brigade == null) return "There is no Brigade " + BrigadeId;
            if (brigade.Faction != Issuer) return "Brigade " + BrigadeId + " belongs to " + brigade.Faction + ", not " + Issuer;
            if (_path.Length == 0) return "A move must enter at least one hex";

            var map = state.Map;
            var perHex = Supply.CostPerHex(state.SectionTypes, brigade);
            long cost = 0;
            var from = brigade.Position;
            foreach (var hex in _path)
            {
                if (from.DistanceTo(hex) != 1) return "Hex " + hex + " is not next to " + from;
                if (!map.Contains(hex)) return "Hex " + hex + " is beyond the map edge (TRN-023)";
                var factor = map.TerrainCostFactor(hex);
                if (factor == null) return "Hex " + hex + " is impassable " + map.TerrainAt(hex).ToString().ToLowerInvariant() + " (TRN-023, TRN-024)";
                var holder = state.Brigades.FirstOrDefault(b => b.Id != brigade.Id && b.Position == hex);
                if (holder != null)
                    return holder.Faction != brigade.Faction
                        ? "Hex " + hex + " is held by an enemy Brigade (TRN-023)"
                        : "Hex " + hex + " is held by Brigade " + holder.Id + "; two Brigades cannot share a hex (TRN-020)";
                cost += (long)perHex * factor.Value;
                from = hex;
            }

            if (brigade.Sections.Any(s => state.SectionTypes.Get(s.TypeId).Slow))
            {
                var cap = state.Parameters.GetInt("P-SUP-10");
                var moved = brigade.HexesMovedIn(state.TurnNumber);
                if (moved + _path.Length > cap)
                    return "A Slow Brigade may move " + cap + " hexes a turn (P-SUP-10); it has moved " + moved + " and this move adds " + _path.Length;
            }

            if (brigade.SP - cost < 0)
                return "Moving " + _path.Length + " hexes costs " + cost + " SP and the Brigade has " + brigade.SP + "; SP may not go below 0 (SUP-006, SUP-015)";
            return null;
        }

        public void Apply(GameState state)
        {
            var brigade = state.Brigades.First(b => b.Id == BrigadeId);
            var perHex = Supply.CostPerHex(state.SectionTypes!, brigade);
            if (brigade.MovedTurn != state.TurnNumber) { brigade.MovedTurn = state.TurnNumber; brigade.MovedHexes = 0; }
            foreach (var hex in _path)
            {
                // TRN-029: SP is taken as each hex is entered.
                brigade.SP -= perHex * state.Map!.TerrainCostFactor(hex)!.Value;
                brigade.Position = hex;
                brigade.MovedHexes++;
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
