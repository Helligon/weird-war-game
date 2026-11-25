using System;

namespace WeirdWar.Spike1.Core
{
    /// <summary>Thrown when a scenario config is malformed or describes an illegal start (DEC-087 stand-ins).</summary>
    public sealed class SetupException : Exception
    {
        public SetupException(string message) : base(message) { }
    }
}
