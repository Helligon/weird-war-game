using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>The contents of a written command log (TEC-005, TEC-008).</summary>
    public sealed class CommandLogContents
    {
        public double StepSeconds { get; }
        /// <summary>How many steps the battle ran.</summary>
        public int Steps { get; }
        public BattleOutcome Outcome { get; }
        public IReadOnlyList<LoggedCommand> Commands { get; }

        public CommandLogContents(double stepSeconds, int steps, BattleOutcome outcome, IReadOnlyList<LoggedCommand> commands)
        {
            StepSeconds = stepSeconds; Steps = steps; Outcome = outcome; Commands = commands;
        }
    }

    /// <summary>
    /// TEC-005, TEC-008, PCH-009, DLV-013: turns a battle's command log into JSON text and back. Pure text in and out: no clock,
    /// no file access, so the core stays deterministic. The format is a spike stand-in, not a rule. Each command carries its
    /// step and its battle time (step times the step length), never wall-clock time.
    /// </summary>
    public static class CommandLogFile
    {
        public static string Write(BattleState battle)
        {
            var sb = new StringBuilder("{\"stepSeconds\":" + Dbl(BattleState.StepSeconds) + ",\"steps\":" + Num(battle.StepCount)
                + ",\"outcome\":\"" + battle.Outcome + "\",\"commands\":[");
            for (var i = 0; i < battle.CommandLog.Count; i++)
            {
                var l = battle.CommandLog[i];
                if (i > 0) sb.Append(',');
                sb.Append("\n{\"step\":").Append(Num(l.Step)).Append(",\"time\":").Append(Dbl(Math.Round(l.Step * BattleState.StepSeconds, 3)))
                    .Append(",\"command\":").Append(CommandSerialiser.Serialise(l.Command)).Append('}');
            }
            return sb.Append("\n]}\n").ToString();
        }

        public static CommandLogContents Read(string json)
        {
            object? parsed;
            try { parsed = MiniJson.Parse(json); }
            catch (DataLoadException e) { throw new CommandFormatException(e.Message); }
            if (!(parsed is Dictionary<string, object?> o)) throw new CommandFormatException("A command log must be a JSON object.");
            var stepSeconds = Number(o, "stepSeconds");
            if (stepSeconds != BattleState.StepSeconds)
                throw new CommandFormatException("The log was made with a step of " + stepSeconds + " s, this build uses " + BattleState.StepSeconds + " s.");
            var steps = Whole(o, "steps");
            if (!(o.TryGetValue("outcome", out var oc) && oc is string name && Enum.TryParse<BattleOutcome>(name, out var outcome)))
                throw new CommandFormatException("\"outcome\" must be a battle outcome name.");
            if (!(o.TryGetValue("commands", out var cs) && cs is List<object?> list))
                throw new CommandFormatException("\"commands\" must be an array.");
            var commands = new List<LoggedCommand>();
            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object?> e)) throw new CommandFormatException("Each log entry must be a JSON object.");
                var step = Whole(e, "step");
                var time = Number(e, "time");
                if (Math.Abs(time - step * stepSeconds) > 0.0005)
                    throw new CommandFormatException("A log entry at step " + step + " has time " + time + ", expected " + step * stepSeconds + ".");
                e.TryGetValue("command", out var c);
                commands.Add(new LoggedCommand(step, CommandSerialiser.FromParsed(c)));
            }
            return new CommandLogContents(stepSeconds, steps, outcome, commands);
        }

        private static int Whole(Dictionary<string, object?> o, string key)
        {
            var v = Number(o, key);
            if (v != Math.Floor(v) || v < 0 || v > int.MaxValue) throw new CommandFormatException("\"" + key + "\" must be a whole number.");
            return (int)v;
        }

        private static double Number(Dictionary<string, object?> o, string key)
        {
            if (o.TryGetValue(key, out var v))
            {
                if (v is long l) return l;
                if (v is double d) return d;
            }
            throw new CommandFormatException("\"" + key + "\" must be a number.");
        }

        private static string Dbl(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
