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

        /// <summary>
        /// The campaign map as one Faction may see it (FOW-001): all terrain (FOW-002), only the locations it has a record of
        /// (FOW-003, FOW-009, FOW-010), its own Brigades in full, enemy Brigades only on hexes it sees now, and its own reserve and CP.
        /// A query: it changes nothing (MUL-014). The result is a copy and holds no reference into the state.
        /// </summary>
        public FactionView ViewFor(GameState state, string faction)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (faction == null) throw new ArgumentNullException(nameof(faction));
            var map = state.Map ?? throw new InvalidOperationException("The game has no map");
            var self = state.Factions.Single(f => f.Id == faction);
            var visible = new HashSet<HexCoord>(VisibleHexes(state, faction));

            var terrain = new List<ViewHex>();
            for (var r = 0; r < map.Height; r++)
                for (var c = 0; c < map.Width; c++)
                {
                    var hex = HexCoord.FromOffset(c, r);
                    terrain.Add(new ViewHex(hex, map.TerrainAt(hex)));
                }

            var locations = new List<ViewLocation>();
            foreach (var l in state.Locations)
            {
                if (visible.Contains(l.Position)) locations.Add(new ViewLocation(l.Id, l.Position, l.Owner, l.Type, true));
                else if (self.KnownLocations.TryGetValue(l.Id, out var k)) locations.Add(new ViewLocation(l.Id, l.Position, k.Owner, k.Type, false));
            }

            var own = state.Brigades.Where(b => b.Faction == faction)
                .Select(b => new ViewOwnBrigade(b.Id, b.SP, b.Position, b.Sections.Select(s => new ViewSection(s.TypeId, s.Health)).ToList()))
                .ToList();
            var enemy = state.Brigades.Where(b => b.Faction != faction && visible.Contains(b.Position))
                .Select(b => new ViewEnemyBrigade(b.Faction, b.Position)).ToList();

            return new FactionView(faction, terrain, locations, own, enemy, self.Reserve, self.Cp);
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
