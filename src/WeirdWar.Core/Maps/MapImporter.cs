using System;
using System.Collections.Generic;
using WeirdWar.Core.Serialization;

namespace WeirdWar.Core.Maps
{
    /// <summary>Thrown when a map file is malformed or invalid. No partial map is ever returned.</summary>
    public sealed class MapFormatException : Exception
    {
        public MapFormatException(string message) : base(message) { }
    }

    /// <summary>Imports the map JSON format described in MapFormat.md (MED-004, MED-006).</summary>
    public static class MapImporter
    {
        public const int CurrentVersion = 1;

        /// <summary>
        /// Technical safety limit per side, so a bad file cannot ask for an enormous allocation.
        /// It is not a game rule: the 50 by 30 target (P-MAP-04) is a design size, not a limit.
        /// </summary>
        public const int MaxSide = 1000;

        /// <summary>BRG-021: 5 Sections is the hard maximum per Brigade.</summary>
        public const int MaxSectionsPerBrigade = 5;

        public static HexMap Parse(string json)
        {
            object? root;
            try { root = Json.Parse(json); }
            catch (JsonException e) { throw new MapFormatException("Map is not valid JSON: " + e.Message); }

            var obj = root as Dictionary<string, object?>
                ?? throw new MapFormatException("Map must be a JSON object");

            var keys = new List<string>(obj.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (var key in keys)
                if (key != "version" && key != "width" && key != "height" && key != "terrain"
                    && key != "factions" && key != "locations" && key != "setup")
                    throw new MapFormatException("Unknown key \"" + key + "\" (expected version, width, height, terrain, factions, locations, setup)");

            var version = ReadInt(obj, "version", 1, int.MaxValue);
            if (version != CurrentVersion)
                throw new MapFormatException("Unsupported map version " + version + " (this build reads version " + CurrentVersion + ")");
            var width = ReadInt(obj, "width", 1, MaxSide);
            var height = ReadInt(obj, "height", 1, MaxSide);

            var cells = new TerrainType[width * height]; // all Open
            var set = new bool[cells.Length];

            if (obj.TryGetValue("terrain", out var terrainValue))
            {
                var terrain = terrainValue as Dictionary<string, object?>
                    ?? throw new MapFormatException("\"terrain\" must be an object of terrain type to hex list");
                // Sorted so the first error reported does not depend on file key order.
                var names = new List<string>(terrain.Keys);
                names.Sort(StringComparer.Ordinal);
                foreach (var name in names)
                {
                    var type = name switch
                    {
                        "open" => TerrainType.Open,
                        "mountain" => TerrainType.Mountain,
                        "sea" => TerrainType.Sea,
                        "river" => TerrainType.River,
                        _ => throw new MapFormatException("Unknown terrain type \"" + name + "\" (expected open, mountain, sea, river)"),
                    };
                    var list = terrain[name] as List<object?>
                        ?? throw new MapFormatException("Terrain \"" + name + "\" must be a list of [column,row] hexes");
                    for (var i = 0; i < list.Count; i++)
                    {
                        var where = "terrain \"" + name + "\" entry " + i;
                        if (!(list[i] is List<object?> pair) || pair.Count != 2 || !(pair[0] is long c) || !(pair[1] is long r))
                            throw new MapFormatException("In " + where + ": expected [column,row] with two whole numbers");
                        if (c < 0 || c >= width || r < 0 || r >= height)
                            throw new MapFormatException("In " + where + ": hex [" + c + "," + r + "] is outside the " + width + "x" + height + " grid");
                        var index = (int)r * width + (int)c;
                        if (set[index])
                            throw new MapFormatException("In " + where + ": hex [" + c + "," + r + "] is listed more than once");
                        set[index] = true;
                        cells[index] = type;
                    }
                }
            }

            var factions = ReadFactions(obj);
            var locations = ReadLocations(obj, width, height, factions);
            var setup = ReadSetup(obj, factions);
            return new HexMap(width, height, cells, factions, locations, setup);
        }

        private static List<string> ReadFactions(Dictionary<string, object?> obj)
        {
            var result = new List<string>();
            if (!obj.TryGetValue("factions", out var v)) return result;
            var list = v as List<object?> ?? throw new MapFormatException("\"factions\" must be a list of Faction ids");
            foreach (var item in list)
            {
                if (!(item is string id) || !MapIds.IsValid(id))
                    throw new MapFormatException("Each Faction id must be a string of letters, digits, - and _");
                if (result.Contains(id)) throw new MapFormatException("Faction \"" + id + "\" is listed more than once");
                result.Add(id);
            }
            if (result.Count != 0 && result.Count != 2)
                throw new MapFormatException("\"factions\" must list exactly two Factions (or be left out for a terrain-only map), got " + result.Count);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static List<Location> ReadLocations(Dictionary<string, object?> obj, int width, int height, List<string> factions)
        {
            var result = new List<Location>();
            var hqCount = new Dictionary<string, int>();
            foreach (var f in factions) hqCount[f] = 0;
            if (obj.TryGetValue("locations", out var v))
            {
                var list = v as List<object?> ?? throw new MapFormatException("\"locations\" must be a list of location objects");
                var ids = new HashSet<string>();
                for (var i = 0; i < list.Count; i++)
                {
                    var where = "location " + i;
                    var o = list[i] as Dictionary<string, object?> ?? throw new MapFormatException("In " + where + ": expected an object");
                    foreach (var key in o.Keys)
                        if (key != "id" && key != "type" && key != "at" && key != "faction")
                            throw new MapFormatException("In " + where + ": unknown key \"" + key + "\" (expected id, type, at, faction)");
                    if (!(o.TryGetValue("id", out var idv) && idv is string id && MapIds.IsValid(id)))
                        throw new MapFormatException("In " + where + ": \"id\" must be a string of letters, digits, - and _");
                    if (!ids.Add(id)) throw new MapFormatException("In " + where + ": id \"" + id + "\" is used more than once");
                    where = "location \"" + id + "\"";
                    var typeName = o.TryGetValue("type", out var tv) ? tv as string : null;
                    LocationType type;
                    switch (typeName)
                    {
                        case "hq": type = LocationType.Hq; break;
                        case "supply": type = LocationType.Supply; break;
                        case "command": type = LocationType.Command; break;
                        case "major": type = LocationType.Major; break;
                        default: throw new MapFormatException("In " + where + ": unknown location type " + (typeName == null ? "(missing or not text)" : "\"" + typeName + "\"") + " (expected hq, supply, command, major)");
                    }
                    if (!(o.TryGetValue("at", out var av) && av is List<object?> pair && pair.Count == 2 && pair[0] is long c && pair[1] is long r))
                        throw new MapFormatException("In " + where + ": \"at\" must be [column,row] with two whole numbers");
                    if (c < 0 || c >= width || r < 0 || r >= height)
                        throw new MapFormatException("In " + where + ": hex [" + c + "," + r + "] is outside the " + width + "x" + height + " grid");
                    string? faction = null;
                    if (type == LocationType.Hq)
                    {
                        faction = o.TryGetValue("faction", out var fv) ? fv as string : null;
                        if (faction == null || !hqCount.ContainsKey(faction))
                            throw new MapFormatException("In " + where + ": an HQ needs \"faction\" naming one of the listed Factions");
                        hqCount[faction]++;
                    }
                    else if (o.ContainsKey("faction"))
                        throw new MapFormatException("In " + where + ": only an HQ has a faction; every ST starts neutral (MAP-021)");
                    result.Add(new Location(id, type, HexCoord.FromOffset((int)c, (int)r), faction));
                }
            }
            foreach (var f in factions)
                if (hqCount[f] != 1)
                    throw new MapFormatException("Faction \"" + f + "\" must have exactly one HQ, found " + hqCount[f] + " (MAP-003)");
            result.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return result;
        }

        private static List<FactionSetup> ReadSetup(Dictionary<string, object?> obj, List<string> factions)
        {
            var result = new List<FactionSetup>();
            var hasSetup = obj.TryGetValue("setup", out var v);
            if (factions.Count == 0)
            {
                if (hasSetup) throw new MapFormatException("\"setup\" needs \"factions\"");
                return result;
            }
            var setup = v as Dictionary<string, object?> ?? throw new MapFormatException("\"setup\" must be an object with one entry per Faction");
            foreach (var key in setup.Keys)
                if (!factions.Contains(key)) throw new MapFormatException("\"setup\" names \"" + key + "\", which is not a listed Faction");
            foreach (var f in factions)
            {
                var where = "setup \"" + f + "\"";
                var o = setup.TryGetValue(f, out var fv) ? fv as Dictionary<string, object?> : null;
                if (o == null) throw new MapFormatException("Missing or bad " + where + ": expected an object with reserve and brigade");
                foreach (var key in o.Keys)
                    if (key != "reserve" && key != "brigade")
                        throw new MapFormatException("In " + where + ": unknown key \"" + key + "\" (expected reserve, brigade)");
                var reserve = ReadInt(o, "reserve", 0, 1000000);
                var b = o.TryGetValue("brigade", out var bv) ? bv as Dictionary<string, object?> : null;
                if (b == null) throw new MapFormatException("In " + where + ": \"brigade\" must be an object with sp and sections");
                foreach (var key in b.Keys)
                    if (key != "sp" && key != "sections")
                        throw new MapFormatException("In " + where + " brigade: unknown key \"" + key + "\" (expected sp, sections)");
                var sp = ReadInt(b, "sp", 0, 1000000);
                var sections = b.TryGetValue("sections", out var sv) ? sv as List<object?> : null;
                if (sections == null || sections.Count == 0 || sections.Count > MaxSectionsPerBrigade)
                    throw new MapFormatException("In " + where + " brigade: \"sections\" must be a list of 1 to " + MaxSectionsPerBrigade + " Section type ids (BRG-021)");
                var ids = new List<string>();
                foreach (var item in sections)
                {
                    if (!(item is string sid) || !MapIds.IsValid(sid))
                        throw new MapFormatException("In " + where + " brigade: each Section must be a Section type id (letters, digits, - and _)");
                    ids.Add(sid);
                }
                result.Add(new FactionSetup(f, reserve, sp, ids));
            }
            return result;
        }

        private static int ReadInt(Dictionary<string, object?> obj, string key, int min, int max)
        {
            if (!obj.TryGetValue(key, out var v)) throw new MapFormatException("Missing required key \"" + key + "\"");
            if (!(v is long n)) throw new MapFormatException("\"" + key + "\" must be a whole number");
            if (n < min || n > max) throw new MapFormatException("\"" + key + "\" must be between " + min + " and " + max + " (got " + n + ")");
            return (int)n;
        }
    }
}
