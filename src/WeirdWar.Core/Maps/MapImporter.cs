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
                if (key != "version" && key != "width" && key != "height" && key != "terrain")
                    throw new MapFormatException("Unknown key \"" + key + "\" (expected version, width, height, terrain)");

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

            return new HexMap(width, height, cells);
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
