using System;
using System.Collections.Generic;

namespace WeirdWar.Core.Maps
{
    /// <summary>The three kinds of ST (MAP-022), plus the HQ (MAP-003, one per Faction).</summary>
    public enum LocationType
    {
        Hq = 0,
        Supply = 1,
        Command = 2,
        Major = 3,
        /// <summary>Only ever made by upgrading a Supply ST (MAP-012, MAP-046); a map file cannot place one (MAP-021).</summary>
        Fob = 4,
    }

    /// <summary>
    /// A location placed by the map file (MAP-003, MAP-022). Immutable. Who owns it is game state, not map data:
    /// only an HQ has a <see cref="Faction"/> in the file; every ST starts neutral (MAP-015, MAP-021).
    /// There is no "supply depot" type (MAP-015).
    /// </summary>
    public sealed class Location
    {
        internal Location(string id, LocationType type, HexCoord position, string? faction)
        {
            Id = id;
            Type = type;
            Position = position;
            Faction = faction;
        }

        public string Id { get; }
        public LocationType Type { get; }
        public HexCoord Position { get; }

        /// <summary>The Faction an HQ belongs to; null for every ST.</summary>
        public string? Faction { get; }
    }

    /// <summary>
    /// Starting setup of one Faction, read from the map file (MAP-021): the Sections of its starting Brigade. They are
    /// named, not rolled (BRG-001's random roll is not built yet). The starting reserve, CP and Brigade SP are not
    /// here: they come from P-RES-05, P-RES-06 and P-BRG-09 (RES-013, BRG-036).
    /// </summary>
    public sealed class FactionSetup
    {
        internal FactionSetup(string factionId, IReadOnlyList<string> sectionTypeIds)
        {
            FactionId = factionId;
            SectionTypeIds = sectionTypeIds;
        }

        public string FactionId { get; }
        public IReadOnlyList<string> SectionTypeIds { get; }
    }

    internal static class MapIds
    {
        /// <summary>Ids are letters, digits, '-' and '_', so canonical text lines cannot be confused by spaces or separators.</summary>
        public static bool IsValid(string id)
        {
            if (id.Length == 0) return false;
            foreach (var ch in id)
                if (!((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_'))
                    return false;
            return true;
        }

        public static string LocationTypeName(LocationType t) => t switch
        {
            LocationType.Hq => "hq",
            LocationType.Supply => "supply",
            LocationType.Command => "command",
            LocationType.Major => "major",
            LocationType.Fob => "fob",
            _ => throw new InvalidOperationException("No name for location type " + t),
        };
    }
}
