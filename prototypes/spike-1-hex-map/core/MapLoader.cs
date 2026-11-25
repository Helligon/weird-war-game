using System;
using System.Collections.Generic;

namespace WeirdWar.Spike1.Core
{
    /// <summary>
    /// Loads a hand-written JSON map (MAP-018; stand-in for the map editor, DEC-087).
    /// The format is described in maps/README.md. Loading is a pure function of the text: no randomness.
    /// </summary>
    public static class MapLoader
    {
        public static HexMap Load(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object?>
                ?? throw new MapLoadException("The map file must be a JSON object.");

            var width = ReadSize(root, "width");
            var height = ReadSize(root, "height");
            var defaultTerrain = ParseTerrain(RequireString(root, "default"), "default terrain");

            var cells = new TerrainType[width * height];
            for (var i = 0; i < cells.Length; i++) cells[i] = defaultTerrain;

            if (root.TryGetValue("terrain", out var terrainValue))
            {
                var overrides = terrainValue as Dictionary<string, object?>
                    ?? throw new MapLoadException("\"terrain\" must be an object of terrain name to cell list.");
                var seen = new HashSet<long>();
                foreach (var pair in overrides)
                {
                    var list = pair.Value as List<object?>
                        ?? throw new MapLoadException("Terrain \"" + pair.Key + "\" must be a list of [column, row] cells.");
                    var cellNote = "terrain \"" + pair.Key + "\"";
                    if (list.Count > 0)
                    {
                        var (c0, r0) = ReadCell(list[0], pair.Key);
                        cellNote = "cell (col " + c0 + ", row " + r0 + ")";
                    }
                    var terrain = ParseTerrain(pair.Key, cellNote);
                    foreach (var item in list)
                    {
                        var (col, row) = ReadCell(item, pair.Key);
                        var where = "cell (col " + col + ", row " + row + ")";
                        if (col < 0 || col >= width || row < 0 || row >= height)
                        {
                            throw new MapLoadException("Map error: " + where + " is outside the " + width + "x" + height + " map.");
                        }
                        if (!seen.Add((long)row * width + col))
                        {
                            throw new MapLoadException("Map error: " + where + " is given more than one terrain.");
                        }
                        cells[row * width + col] = terrain;
                    }
                }
            }
            return new HexMap(width, height, cells);
        }

        private static int ReadSize(Dictionary<string, object?> root, string key)
        {
            if (!root.TryGetValue(key, out var v) || !(v is long n) || n < 1 || n > 10000)
            {
                throw new MapLoadException("\"" + key + "\" must be a whole number from 1 to 10000.");
            }
            return (int)n;
        }

        private static string RequireString(Dictionary<string, object?> root, string key)
        {
            if (!root.TryGetValue(key, out var v) || !(v is string s))
            {
                throw new MapLoadException("\"" + key + "\" must be a string.");
            }
            return s;
        }

        private static (long col, long row) ReadCell(object? item, string terrainName)
        {
            if (item is List<object?> pair && pair.Count == 2 && pair[0] is long c && pair[1] is long r)
            {
                return (c, r);
            }
            throw new MapLoadException("Map error: a cell under \"" + terrainName + "\" is not a [column, row] pair of whole numbers.");
        }

        private static TerrainType ParseTerrain(string name, string what)
        {
            switch (name)
            {
                case "plain": return TerrainType.Plain;
                case "mountain": return TerrainType.Mountain;
                case "sea": return TerrainType.Sea;
                case "river": return TerrainType.River;
                default:
                    throw new MapLoadException("Map error: unknown terrain type \"" + name + "\" (" + what + ").");
            }
        }
    }
}
