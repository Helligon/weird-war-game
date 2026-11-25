using System;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// A Move waypoint: a battle position in whole metres. Spike stand-in: Q is metres east (map X) and R is metres north
    /// (map Y), the same axes as <see cref="MapPoint"/>. Replace it with a real position type when the tactical view lands.
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
