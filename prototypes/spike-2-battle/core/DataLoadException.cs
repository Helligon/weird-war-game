using System;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>Thrown when a hand-written data file (map or stats) is malformed. The message says what to fix.</summary>
    public sealed class DataLoadException : Exception
    {
        public DataLoadException(string message) : base(message) { }
    }
}
