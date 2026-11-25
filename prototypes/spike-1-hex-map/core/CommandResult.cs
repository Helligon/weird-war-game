using System;

namespace WeirdWar.Spike1.Core
{
    /// <summary>
    /// Outcome of applying a command (DLV-013): either the new state, or a rejection with a reason and the
    /// state unchanged. Check <see cref="IsAccepted"/> first.
    /// </summary>
    public sealed class CommandResult
    {
        public bool IsAccepted { get; }

        /// <summary>The state after the command. For a rejection this is the original state, unchanged.</summary>
        public GameState State { get; }

        /// <summary>Why the command was rejected; null when accepted.</summary>
        public string? RejectionReason { get; }

        private CommandResult(bool accepted, GameState state, string? reason)
        {
            IsAccepted = accepted;
            State = state;
            RejectionReason = reason;
        }

        internal static CommandResult Accept(GameState next) => new CommandResult(true, next, null);

        internal static CommandResult Reject(GameState unchanged, string reason)
        {
            if (string.IsNullOrEmpty(reason)) throw new ArgumentException("A rejection needs a reason.", nameof(reason));
            return new CommandResult(false, unchanged, reason);
        }
    }
}
