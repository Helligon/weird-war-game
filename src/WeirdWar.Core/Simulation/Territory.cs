using System;
using System.Collections.Generic;
using System.Linq;
using WeirdWar.Core.Maps;

namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// Territory (MAP-016, MAP-027). A Faction's territory is the hexes around the locations it owns: each owned
    /// location claims every on-map hex within its radius, its own hex included. Only owned locations claim (a neutral
    /// location claims nothing) and Brigades never do (CC1 does not model Brigade territory; MAP-028 is proposed).
    /// Terrain does not matter; the map edge clips the area.
    /// Reads its values by parameter ID (DLV-014), all at construction, so a missing or malformed value fails at game start:
    ///   P-FOW-03: sight radius per location type, as "hq=3 fob=2 major=2 command=1 supply=1".
    ///   P-MAP-02: radius = sight - minus, but at least min, as "minus=1 min=1".
    /// </summary>
    public sealed class TerritoryRules
    {
        private readonly Dictionary<LocationType, int> _radius = new Dictionary<LocationType, int>();

        public TerritoryRules(ParameterTable parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            var sight = parameters.GetKeyedInts("P-FOW-03", "hq", "fob", "major", "command", "supply");
            var rule = parameters.GetKeyedInts("P-MAP-02", "minus", "min");
            foreach (var kv in sight)
                if (kv.Value < 0) throw new ParameterFormatException("P-FOW-03: '" + kv.Key + "' must not be negative.");
            foreach (var kv in rule)
                if (kv.Value < 0) throw new ParameterFormatException("P-MAP-02: '" + kv.Key + "' must not be negative.");
            int Radius(string key) => Math.Max(sight[key] - rule["minus"], rule["min"]);
            _radius[LocationType.Hq] = Radius("hq");
            _radius[LocationType.Major] = Radius("major");
            _radius[LocationType.Command] = Radius("command");
            _radius[LocationType.Supply] = Radius("supply");
            _radius[LocationType.Fob] = Radius("fob");
        }

        /// <summary>The territory radius of a location type (MAP-027): its sight minus P-MAP-02's amount, never below the minimum.</summary>
        public int RadiusOf(LocationType type) => _radius[type];

        /// <summary>
        /// The hexes of <paramref name="faction"/>'s territory (MAP-016): every on-map hex within the radius of a location it
        /// owns. No duplicates, sorted by (Q, R) so the answer is deterministic. Reads the state and never changes it.
        /// </summary>
        public IReadOnlyList<HexCoord> HexesOf(GameState state, string faction)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var map = state.Map ?? throw new InvalidOperationException("There is no map, so there is no territory");
            var hexes = new HashSet<HexCoord>();
            foreach (var l in state.Locations)
            {
                if (l.Owner != faction) continue;
                var radius = _radius[l.Type];
                for (var dq = -radius; dq <= radius; dq++)
                    for (var dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                    {
                        var hex = new HexCoord(l.Position.Q + dq, l.Position.R + dr);
                        if (map.Contains(hex)) hexes.Add(hex);
                    }
            }
            return hexes.OrderBy(h => h.Q).ThenBy(h => h.R).ToList();
        }
    }
}
