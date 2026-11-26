using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using WeirdWar.Core.Commands;
using WeirdWar.Core.Serialization;

namespace WeirdWar.Core.Simulation
{
    /// <summary>Outcome of applying a command: accepted, or rejected with a reason.</summary>
    public sealed class CommandResult
    {
        private CommandResult(bool accepted, string? reason) { Accepted = accepted; Reason = reason; }

        public bool Accepted { get; }
        public string? Reason { get; }

        public static CommandResult Accept() => new CommandResult(true, null);
        public static CommandResult Reject(string reason) => new CommandResult(false, reason);
    }

    /// <summary>Thrown by replay when a log does not reproduce: a rejected command or an out-of-order entry.</summary>
    public sealed class ReplayDivergenceException : Exception
    {
        public ReplayDivergenceException(string message) : base(message) { }
    }

    /// <summary>
    /// The whole game state (DLV-012, TEC-008). Deliberately minimal for now: the seed, the single seeded
    /// random source, the command log and a trivial tally for test commands. Later Stories add fields, and
    /// MUST add each one to <see cref="CanonicalText"/> so it is part of the hash.
    /// State changes only through <see cref="ApplyCommand"/> (DLV-013).
    /// </summary>
    public sealed class GameState
    {
        public GameState(ulong seed)
        {
            Seed = seed;
            Random = new SeededRandom(seed);
        }

        public ulong Seed { get; }

        /// <summary>The one random source. All random draws in the simulation MUST come from here (TEC-012).</summary>
        public SeededRandom Random { get; }

        public CommandLog Log { get; } = new CommandLog();

        /// <summary>Trivial value changed by the test commands.</summary>
        public long Tally { get; internal set; }

        /// <summary>
        /// The single apply path. Validates first; a rejected command changes nothing and is not logged.
        /// An accepted command is applied and then appended to the log.
        /// </summary>
        public CommandResult ApplyCommand(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var reason = command.Validate(this);
            if (reason != null) return CommandResult.Reject(reason);
            command.Apply(this);
            Log.Append(command);
            return CommandResult.Accept();
        }

        /// <summary>
        /// Rebuilds a state from a seed and a log. Throws <see cref="ReplayDivergenceException"/> if an entry
        /// is out of order (step is not its position) or a command is rejected.
        /// </summary>
        public static GameState Replay(ulong seed, CommandLog log)
        {
            var state = new GameState(seed);
            for (int i = 0; i < log.Entries.Count; i++)
            {
                var entry = log.Entries[i];
                if (entry.Step != i)
                    throw new ReplayDivergenceException("Log entry at position " + i + " has step " + entry.Step);
                var result = state.ApplyCommand(entry.Command);
                if (!result.Accepted)
                    throw new ReplayDivergenceException("Command at step " + i + " was rejected: " + result.Reason);
            }
            return state;
        }

        /// <summary>
        /// Canonical text of the state: fixed field order, invariant formatting, commands as canonical JSON.
        /// No dictionary order, object identity or per-process hash codes.
        /// </summary>
        public string CanonicalText()
        {
            var sb = new StringBuilder();
            sb.Append("weirdwar.state.v1\n");
            sb.Append("seed=").Append(Seed.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("random=").Append(Random.Position.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("tally=").Append(Tally.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("log=").Append(Log.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var e in Log.Entries)
            {
                var fields = e.Command.ToJson();
                fields["type"] = e.Command.Type;
                sb.Append(e.Step.ToString(CultureInfo.InvariantCulture)).Append(':').Append(Json.Write(fields)).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>SHA-256 of <see cref="CanonicalText"/> (UTF-8), as lowercase hex.</summary>
        public string ComputeHash()
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(CanonicalText()));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }
    }
}
