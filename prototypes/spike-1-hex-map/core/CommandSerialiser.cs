using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WeirdWar.Spike1.Core
{
    /// <summary>
    /// Saves commands to JSON and reads them back so a command list can be stored and replayed (DLV-013, TEC-008).
    /// Only plain data is written: the command type and its parameters. Formats (fixed key order):
    /// <c>{"type":"move","player":1,"tokenOwner":1,"q":Q,"r":R}</c> (q, r = axial target) and
    /// <c>{"type":"endTurn","player":1}</c>. A list is a JSON array of these.
    /// Add new command types in <see cref="Serialise(GameCommand)"/> and <see cref="Deserialise"/> together.
    /// </summary>
    public static class CommandSerialiser
    {
        public static string Serialise(GameCommand command)
        {
            switch (command)
            {
                case MoveCommand m:
                    return "{\"type\":\"move\",\"player\":" + Num((int)m.Player) + ",\"tokenOwner\":" + Num((int)m.TokenOwner)
                        + ",\"q\":" + Num(m.Target.Q) + ",\"r\":" + Num(m.Target.R) + "}";
                case EndTurnCommand e:
                    return "{\"type\":\"endTurn\",\"player\":" + Num((int)e.Player) + "}";
                default:
                    throw new CommandFormatException("Cannot save unknown command type " + (command == null ? "null" : command.GetType().Name) + ".");
            }
        }

        public static string SerialiseList(IEnumerable<GameCommand> commands)
        {
            var sb = new StringBuilder("[");
            var first = true;
            foreach (var c in commands)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append(Serialise(c));
            }
            return sb.Append(']').ToString();
        }

        public static GameCommand Deserialise(string json) => FromObject(Parse(json));

        public static List<GameCommand> DeserialiseList(string json)
        {
            if (!(Parse(json) is List<object?> items))
            {
                throw new CommandFormatException("A command list must be a JSON array.");
            }
            var result = new List<GameCommand>(items.Count);
            foreach (var item in items)
            {
                result.Add(FromObject(item));
            }
            return result;
        }

        private static object? Parse(string json)
        {
            try
            {
                return MiniJson.Parse(json);
            }
            catch (MapLoadException e)
            {
                throw new CommandFormatException("Command error: " + e.Message);
            }
        }

        private static GameCommand FromObject(object? value)
        {
            if (!(value is Dictionary<string, object?> o))
            {
                throw new CommandFormatException("A command must be a JSON object.");
            }
            if (!o.TryGetValue("type", out var type) || !(type is string name))
            {
                throw new CommandFormatException("A command needs a \"type\".");
            }
            var player = ReadPlayer(o, "player");
            switch (name)
            {
                case "move":
                    return new MoveCommand(player, ReadPlayer(o, "tokenOwner"), new HexCoord(ReadInt(o, "q"), ReadInt(o, "r")));
                case "endTurn":
                    return new EndTurnCommand(player);
                default:
                    throw new CommandFormatException("Unknown command type \"" + name + "\".");
            }
        }

        private static PlayerId ReadPlayer(Dictionary<string, object?> o, string key)
        {
            var n = ReadInt(o, key);
            if (n != 1 && n != 2)
            {
                throw new CommandFormatException("\"" + key + "\" must be 1 or 2.");
            }
            return (PlayerId)n;
        }

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
