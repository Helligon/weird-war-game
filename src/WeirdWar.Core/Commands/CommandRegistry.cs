using System;
using System.Collections.Generic;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Commands
{
    /// <summary>
    /// Maps command type names to readers, and turns commands and logs to and from JSON text (TEC-008).
    /// An instance, not a global: later Stories register their commands in <see cref="CreateDefault"/>.
    /// </summary>
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string, Func<IReadOnlyDictionary<string, object?>, ICommand>> _readers =
            new Dictionary<string, Func<IReadOnlyDictionary<string, object?>, ICommand>>(StringComparer.Ordinal);

        public static CommandRegistry CreateDefault()
        {
            var r = new CommandRegistry();
            r.Register(AddToTallyCommand.TypeName, AddToTallyCommand.FromJson);
            r.Register(RollIntoTallyCommand.TypeName, RollIntoTallyCommand.FromJson);
            return r;
        }

        public void Register(string type, Func<IReadOnlyDictionary<string, object?>, ICommand> reader)
        {
            if (_readers.ContainsKey(type)) throw new ArgumentException("Command type already registered: " + type);
            _readers[type] = reader;
        }

        public string WriteCommand(ICommand command) => Json.Write(ToValue(command));

        public ICommand ReadCommand(string json) => FromValue(Json.Parse(json));

        public string WriteLog(CommandLog log)
        {
            var list = new List<object?>();
            foreach (var e in log.Entries)
                list.Add(new Dictionary<string, object?> { ["step"] = (long)e.Step, ["command"] = ToValue(e.Command) });
            return Json.Write(list);
        }

        public CommandLog ReadLog(string json)
        {
            if (!(Json.Parse(json) is List<object?> list)) throw new JsonException("A command log must be a JSON array");
            var entries = new List<LogEntry>();
            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object?> d)) throw new JsonException("A log entry must be a JSON object");
                var step = RequireLong(d, "step");
                if (step < 0 || step > int.MaxValue) throw new JsonException("Field 'step' out of range");
                entries.Add(new LogEntry((int)step, FromValue(Require(d, "command"))));
            }
            return CommandLog.FromEntries(entries);
        }

        private static Dictionary<string, object?> ToValue(ICommand command)
        {
            var d = command.ToJson();
            if (d.ContainsKey("type")) throw new InvalidOperationException("'type' is reserved by the serialiser");
            d["type"] = command.Type;
            return d;
        }

        private ICommand FromValue(object? value)
        {
            if (!(value is Dictionary<string, object?> d)) throw new JsonException("A command must be a JSON object");
            if (!(Require(d, "type") is string type)) throw new JsonException("Field 'type' must be a string");
            if (!_readers.TryGetValue(type, out var reader)) throw new JsonException("Unknown command type '" + type + "'");
            return reader(d);
        }

        private static object? Require(IReadOnlyDictionary<string, object?> json, string key)
        {
            if (!json.TryGetValue(key, out var v)) throw new JsonException("Missing field '" + key + "'");
            return v;
        }

        /// <summary>Reads a required whole-number field. For use by command readers.</summary>
        public static long RequireLong(IReadOnlyDictionary<string, object?> json, string key)
        {
            if (Require(json, key) is long l) return l;
            throw new JsonException("Field '" + key + "' must be a whole number");
        }
    }
}
