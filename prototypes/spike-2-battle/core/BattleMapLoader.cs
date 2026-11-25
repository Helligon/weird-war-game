using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// Loads the one hand-made battle map from JSON (DEC-087 stand-in; BMP-001 and BMP-009 are not implemented).
    /// Format: { "width": metres, "height": metres, "cover": [ { "id": "...", "x": m, "y": m, "radius": m } ] }.
    /// The west and east edges are the map's short sides.
    /// </summary>
    public static class BattleMapLoader
    {
        public static BattleMap Load(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object?>
                ?? throw new DataLoadException("The map file must be a JSON object.");
            var width = Number(root, "width", "map");
            var height = Number(root, "height", "map");
            if (width <= 0 || height <= 0) throw new DataLoadException("Map error: \"width\" and \"height\" must be above zero.");

            var cover = new List<CoverObject>();
            if (root.TryGetValue("cover", out var coverValue))
            {
                var list = coverValue as List<object?> ?? throw new DataLoadException("\"cover\" must be a list.");
                var ids = new HashSet<string>();
                foreach (var item in list)
                {
                    var o = item as Dictionary<string, object?> ?? throw new DataLoadException("Each \"cover\" entry must be an object.");
                    if (!o.TryGetValue("id", out var idValue) || !(idValue is string id) || id.Length == 0)
                        throw new DataLoadException("Map error: each cover object needs a text \"id\".");
                    if (!ids.Add(id)) throw new DataLoadException("Map error: cover id \"" + id + "\" is used twice.");
                    var what = "cover \"" + id + "\"";
                    var x = Number(o, "x", what);
                    var y = Number(o, "y", what);
                    var radius = Number(o, "radius", what);
                    if (radius <= 0) throw new DataLoadException("Map error: " + what + " needs a radius above zero.");
                    var strip = BattleMap.EdgeStripWidth;
                    if (x - radius < strip || x + radius > width - strip || y - radius < 0 || y + radius > height)
                        throw new DataLoadException("Map error: " + what + " must lie wholly inside the " + width + " x " + height
                            + " map and clear of the " + strip + " m edge strips.");
                    cover.Add(new CoverObject(id, new MapPoint(x, y), radius));
                }
            }
            return new BattleMap(width, height, cover);
        }

        internal static double Number(Dictionary<string, object?> o, string key, string what)
        {
            if (!o.TryGetValue(key, out var v)) throw new DataLoadException("Missing \"" + key + "\" in " + what + ".");
            if (v is long l) return l;
            if (v is double d) return d;
            throw new DataLoadException("\"" + key + "\" in " + what + " must be a number.");
        }
    }
}
