using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <summary>Format tag written into every log. Bump it when the log layout changes.</summary>
        public const string LogFormat = "weirdwar.log.v1";

        private readonly Dictionary<string, Func<IReadOnlyDictionary<string, object?>, ICommand>> _readers =
            new Dictionary<string, Func<IReadOnlyDictionary<string, object?>, ICommand>>(StringComparer.Ordinal);

        public static CommandRegistry CreateDefault()
        {
            var r = new CommandRegistry();
            r.Register(AddToTallyCommand.TypeName, AddToTallyCommand.FromJson);
            r.Register(RollIntoTallyCommand.TypeName, RollIntoTallyCommand.FromJson);
            r.Register(EndTurnCommand.TypeName, EndTurnCommand.FromJson);
            r.Register(DistributeSpCommand.TypeName, DistributeSpCommand.FromJson);
            r.Register(MoveBrigadeCommand.TypeName, MoveBrigadeCommand.FromJson);
            return r;
        }

        public void Register(string type, Func<IReadOnlyDictionary<string, object?>, ICommand> reader)
        {
            if (_readers.ContainsKey(type)) throw new ArgumentException("Command type already registered: " + type);
            _readers[type] = reader;
        }

        public bool IsRegistered(string type) => _readers.ContainsKey(type);

        public string WriteCommand(ICommand command) => CommandJson.Canonical(command);

        public ICommand ReadCommand(string json) => FromValue(Json.Parse(json));

        /// <summary>
        /// Writes a run as {"format","seed","entries"}. The seed is a decimal string, because a 64-bit
        /// number does not survive every JSON reader.
        /// </summary>
        public string WriteLog(ulong seed, CommandLog log)
        {
            var entries = new List<object?>();
            foreach (var e in log.Entries)
                entries.Add(new Dictionary<string, object?> { ["step"] = (long)e.Step, ["command"] = CommandJson.ToValue(e.Command) });
            return Json.Write(new Dictionary<string, object?>
            {
                ["format"] = LogFormat,
                ["seed"] = seed.ToString(CultureInfo.InvariantCulture),
                ["entries"] = entries,
            });
        }

        public LoggedRun ReadLog(string json)
        {
            if (!(Json.Parse(json) is Dictionary<string, object?> d)) throw new JsonException("A command log must be a JSON object");
            if (!(Require(d, "format") is string format) || format != LogFormat)
                throw new JsonException("Unsupported log format (expected '" + LogFormat + "')");
            if (!(Require(d, "seed") is string seedText)
                || !ulong.TryParse(seedText, NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
                throw new JsonException("Field 'seed' must be a decimal string");
            if (!(Require(d, "entries") is List<object?> list)) throw new JsonException("Field 'entries' must be an array");
            var entries = new List<LogEntry>();
            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object?> e)) throw new JsonException("A log entry must be a JSON object");
                var step = RequireLong(e, "step");
                if (step < 0 || step > int.MaxValue) throw new JsonException("Field 'step' out of range");
                entries.Add(new LogEntry((int)step, FromValue(Require(e, "command"))));
            }
            return new LoggedRun(seed, CommandLog.FromEntries(entries));
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

        /// <summary>Adds the "faction" field (the issuer) unless it is empty. For use by command writers.</summary>
        public static Dictionary<string, object?> WithIssuer(Dictionary<string, object?> json, string issuer)
        {
            if (issuer.Length > 0) json["faction"] = issuer;
            return json;
        }

        /// <summary>Reads the "faction" field, or "" when absent. For use by command readers.</summary>
        public static string OptionalIssuer(IReadOnlyDictionary<string, object?> json)
        {
            if (!json.TryGetValue("faction", out var v)) return "";
            if (v is string s) return s;
            throw new JsonException("Field 'faction' must be a string");
        }

        /// <summary>Reads a required whole-number field. For use by command readers.</summary>
        public static long RequireLong(IReadOnlyDictionary<string, object?> json, string key)
        {
            if (Require(json, key) is long l) return l;
            throw new JsonException("Field '" + key + "' must be a whole number");
        }
    }

    /// <summary>A seed and the log of accepted commands that, replayed, rebuild a state.</summary>
    public sealed class LoggedRun
    {
        public LoggedRun(ulong seed, CommandLog log)
        {
            Seed = seed;
            Log = log;
        }

        public ulong Seed { get; }
        public CommandLog Log { get; }
    }
}
