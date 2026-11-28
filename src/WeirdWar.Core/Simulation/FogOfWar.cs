using System;
using System.Collections.Generic;
using System.Linq;
using WeirdWar.Core.Maps;

namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// Fog of war queries (FOW-004, FOW-006, FOW-007). Read-only: it changes no state and nothing here is hashed (MUL-014, TEC-008).
    /// Built once at game start from the Section type data.
    /// </summary>
    public sealed class FogOfWar
    {
        private readonly SectionTypeTable _types;

        public FogOfWar(SectionTypeTable sectionTypes)
        {
            _types = sectionTypes ?? throw new ArgumentNullException(nameof(sectionTypes));
        }

        /// <summary>
        /// A Brigade's sight in hexes: the best <see cref="SectionType.Sight"/> among its current Sections (FOW-006).
        /// Returns 0 for a Brigade with no Sections. That case cannot happen in play, because BRG-023 disbands an empty Brigade.
        /// </summary>
        public int BrigadeSight(Brigade brigade)
        {
            if (brigade == null) throw new ArgumentNullException(nameof(brigade));
            return brigade.Sections.Select(s => _types.Get(s.TypeId).Sight).DefaultIfEmpty(0).Max();
        }

        /// <summary>
        /// The on-map hexes a Faction can see (FOW-004, FOW-006, FOW-007), each once, sorted by (Q, R). Sources are its own
        /// Brigades (their sight, centred on the Brigade) and the locations it owns (P-FOW-03, centred on the location).
        /// Sight is plain hex distance: terrain does not block it. Other Factions' sources add nothing (FOW-003).
        /// Derived from the state on every call; it changes nothing and stores nothing (MUL-014, TEC-008).
        /// </summary>
        public IReadOnlyList<HexCoord> VisibleHexes(GameState state, string faction)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (faction == null) throw new ArgumentNullException(nameof(faction));
            var map = state.Map;
            var locationSight = state.LocationSight;
            var seen = new HashSet<HexCoord>();
            if (map == null || locationSight == null) return new List<HexCoord>();
            foreach (var b in state.Brigades)
                if (b.Faction == faction) AddArea(map, seen, b.Position, BrigadeSight(b));
            foreach (var l in state.Locations)
                if (l.Owner == faction) AddArea(map, seen, l.Position, locationSight.RadiusOf(l));
            var result = seen.ToList();
            result.Sort((a, b) => a.Q != b.Q ? a.Q.CompareTo(b.Q) : a.R.CompareTo(b.R));
            return result;
        }

        private static void AddArea(HexMap map, HashSet<HexCoord> seen, HexCoord centre, int radius)
        {
            for (var dq = -radius; dq <= radius; dq++)
                for (var dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                {
                    var hex = new HexCoord(centre.Q + dq, centre.R + dr);
                    if (map.Contains(hex)) seen.Add(hex);
                }
        }
    }
}
