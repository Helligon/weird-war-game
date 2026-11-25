using System;

namespace WeirdWar.Spike1.Core
{
    /// <summary>A saved command or command list could not be read (TEC-008).</summary>
    public sealed class CommandFormatException : Exception
    {
        public CommandFormatException(string message) : base(message) { }
    }
}
