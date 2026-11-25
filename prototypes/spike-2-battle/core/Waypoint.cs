using System;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// A Move waypoint as an axial hex cell. Stand-in: the map (Story #36) is not in the core yet, so this
    /// only carries the two numbers. Replace or map it to the real hex type when the map lands.
    /// </summary>
    public readonly struct Waypoint : IEquatable<Waypoint>
    {
        public int Q { get; }
        public int R { get; }

        public Waypoint(int q, int r) { Q = q; R = r; }

        public bool Equals(Waypoint other) => Q == other.Q && R == other.R;
        public override bool Equals(object? obj) => obj is Waypoint o && Equals(o);
        public override int GetHashCode() => unchecked(Q * 397 ^ R);
    }
}
