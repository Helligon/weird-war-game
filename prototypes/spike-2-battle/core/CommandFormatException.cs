using System;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>A saved command, or a data file, could not be read (TEC-008).</summary>
    public sealed class CommandFormatException : Exception
    {
        public CommandFormatException(string message) : base(message) { }
    }
}
