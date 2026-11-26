using System;
using System.Collections.Generic;
using WeirdWar.Core.Commands;

namespace WeirdWar.Core.Simulation
{
    /// <summary>One accepted command and the step at which it was applied (0 for the first, then 1, 2...).</summary>
    public sealed class LogEntry
    {
        public LogEntry(int step, ICommand command)
        {
            Step = step;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public int Step { get; }
        public ICommand Command { get; }
    }

    /// <summary>
    /// The ordered record of accepted commands (DLV-013, TEC-008). Only <see cref="GameState"/> appends;
    /// rejected commands never appear here.
    /// </summary>
    public sealed class CommandLog
    {
        private readonly List<LogEntry> _entries = new List<LogEntry>();

        public IReadOnlyList<LogEntry> Entries => _entries;
        public int Count => _entries.Count;

        internal void Append(ICommand command) { _entries.Add(new LogEntry(_entries.Count, command)); }

        /// <summary>Builds a log from already-recorded entries (e.g. read from a save). Steps are checked on replay, not here.</summary>
        public static CommandLog FromEntries(IEnumerable<LogEntry> entries)
        {
            var log = new CommandLog();
            log._entries.AddRange(entries);
            return log;
        }
    }
}
