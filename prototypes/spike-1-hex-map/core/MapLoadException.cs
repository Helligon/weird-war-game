using System;

namespace WeirdWar.Spike1.Core
{
    /// <summary>Thrown when a map file is malformed. The message names the offending cell where there is one.</summary>
    public sealed class MapLoadException : Exception
    {
        public MapLoadException(string message) : base(message) { }
    }
}
