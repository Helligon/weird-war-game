using System;
using System.Collections.Generic;
using System.Linq;
using WeirdWar.Core.Maps;

namespace WeirdWar.Core.Simulation
{
    /// <summary>One map hex and its terrain (FOW-002).</summary>
    public sealed class ViewHex
    {
        internal ViewHex(HexCoord position, TerrainType terrain) { Position = position; Terrain = terrain; }
        public HexCoord Position { get; }
        public TerrainType Terrain { get; }
    }

    /// <summary>A location a Faction has a record of (FOW-009, FOW-010). Owner and type are as last seen unless <see cref="InSightNow"/>.</summary>
    public sealed class ViewLocation
    {
        internal ViewLocation(string id, HexCoord position, string? owner, LocationType type, bool inSightNow)
        { Id = id; Position = position; Owner = owner; Type = type; InSightNow = inSightNow; }
        public string Id { get; }
        public HexCoord Position { get; }
        /// <summary>The last-known owner (null for neutral).</summary>
        public string? Owner { get; }
        public LocationType Type { get; }
        public bool InSightNow { get; }
    }

    /// <summary>A Section of the Faction's own Brigade, copied.</summary>
    public sealed class ViewSection
    {
        internal ViewSection(string typeId, int health) { TypeId = typeId; Health = health; }
        public string TypeId { get; }
        public int Health { get; }
    }

    /// <summary>One of the Faction's own Brigades, in full and copied.</summary>
    public sealed class ViewOwnBrigade
    {
        internal ViewOwnBrigade(int id, int sp, HexCoord position, IReadOnlyList<ViewSection> sections)
        { Id = id; SP = sp; Position = position; Sections = sections; }
        public int Id { get; }
        public int SP { get; }
        public HexCoord Position { get; }
        public IReadOnlyList<ViewSection> Sections { get; }
    }

    /// <summary>An enemy Brigade seen now: only its Faction and hex (FOW-003).</summary>
    public sealed class ViewEnemyBrigade
    {
        internal ViewEnemyBrigade(string faction, HexCoord position) { Faction = faction; Position = position; }
        public string Faction { get; }
        public HexCoord Position { get; }
    }

    /// <summary>
    /// What one Faction may see of the campaign map (FOW-001). A snapshot: it holds no reference into the live state, so later
    /// changes do not alter it (MUL-014, TEC-008).
    /// </summary>
    public sealed class FactionView
    {
        internal FactionView(string faction, IReadOnlyList<ViewHex> terrain, IReadOnlyList<ViewLocation> locations,
            IReadOnlyList<ViewOwnBrigade> ownBrigades, IReadOnlyList<ViewEnemyBrigade> enemyBrigades, int reserve, int cp)
        {
            Faction = faction; Terrain = terrain; Locations = locations; OwnBrigades = ownBrigades; EnemyBrigades = enemyBrigades; Reserve = reserve; Cp = cp;
        }
        public string Faction { get; }
        /// <summary>Every hex of the map (FOW-002).</summary>
        public IReadOnlyList<ViewHex> Terrain { get; }
        /// <summary>Only locations this Faction has a record of, by id (FOW-003, FOW-010).</summary>
        public IReadOnlyList<ViewLocation> Locations { get; }
        public IReadOnlyList<ViewOwnBrigade> OwnBrigades { get; }
        /// <summary>Enemy Brigades on hexes the Faction sees now, in Brigade id order.</summary>
        public IReadOnlyList<ViewEnemyBrigade> EnemyBrigades { get; }
        /// <summary>The Faction's own reserve. Other Factions' reserve is not in the view.</summary>
        public int Reserve { get; }
        /// <summary>The Faction's own CP. Other Factions' CP is not in the view.</summary>
        public int Cp { get; }
    }

}
