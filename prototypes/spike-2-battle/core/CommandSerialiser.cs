using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// Saves commands to JSON and reads them back (DLV-013, TEC-008). The format is a spike stand-in, not a rule.
    /// Only plain data is written, with a fixed key order: <c>{"type":"move","section":1,"waypoints":[[q,r],...]}</c>,
    /// <c>{"type":"target","section":1,"target":2}</c>, and <c>{"type":"stop"|"clearWaypoints"|"fallBack","section":1}</c>.
    /// </summary>
    public static class CommandSerialiser
    {
        public static string Serialise(IBattleCommand command)
        {
            switch (command)
            {
                case MoveCommand m:
                    var sb = new StringBuilder("{\"type\":\"move\",\"section\":" + Num(m.Section.Value) + ",\"waypoints\":[");
                    for (var i = 0; i < m.Waypoints.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append('[').Append(Num(m.Waypoints[i].Q)).Append(',').Append(Num(m.Waypoints[i].R)).Append(']');
                    }
                    return sb.Append("]}").ToString();
                case TargetCommand t:
                    return Simple("target", t.Section) + ",\"target\":" + Num(t.TargetSection.Value) + "}";
                case StopCommand s:
                    return Simple("stop", s.Section) + "}";
                case ClearWaypointsCommand c:
                    return Simple("clearWaypoints", c.Section) + "}";
                case FallBackCommand f:
                    return Simple("fallBack", f.Section) + "}";
                default:
                    throw new CommandFormatException("Cannot save unknown command type " + (command == null ? "null" : command.GetType().Name) + ".");
            }
        }

        public static IBattleCommand Deserialise(string json)
        {
            object? parsed;
            try { parsed = MiniJson.Parse(json); }
            catch (DataLoadException e) { throw new CommandFormatException(e.Message); }
            if (!(parsed is Dictionary<string, object?> o))
            {
                throw new CommandFormatException("A command must be a JSON object.");
            }
            if (!o.TryGetValue("type", out var type) || !(type is string name))
            {
                throw new CommandFormatException("A command needs a \"type\".");
            }
            var section = new SectionId(ReadInt(o, "section"));
            switch (name)
            {
                case "move":
                    if (!o.TryGetValue("waypoints", out var w) || !(w is List<object?> list))
                    {
                        throw new CommandFormatException("\"waypoints\" must be an array.");
                    }
                    var points = new List<Waypoint>();
                    foreach (var item in list)
                    {
                        if (!(item is List<object?> pair) || pair.Count != 2 || !(pair[0] is long q) || !(pair[1] is long r)
                            || q < int.MinValue || q > int.MaxValue || r < int.MinValue || r > int.MaxValue)
                        {
                            throw new CommandFormatException("Each waypoint must be a [q, r] pair of whole numbers.");
                        }
                        points.Add(new Waypoint((int)q, (int)r));
                    }
                    return new MoveCommand(section, points);
                case "target": return new TargetCommand(section, new SectionId(ReadInt(o, "target")));
                case "stop": return new StopCommand(section);
                case "clearWaypoints": return new ClearWaypointsCommand(section);
                case "fallBack": return new FallBackCommand(section);
                default: throw new CommandFormatException("Unknown command type \"" + name + "\".");
            }
        }

        private static string Simple(string type, SectionId section) =>
            "{\"type\":\"" + type + "\",\"section\":" + Num(section.Value);

        private static int ReadInt(Dictionary<string, object?> o, string key)
        {
            if (o.TryGetValue(key, out var v) && v is long l && l >= int.MinValue && l <= int.MaxValue)
            {
                return (int)l;
            }
            throw new CommandFormatException("\"" + key + "\" must be a whole number.");
        }

        private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
