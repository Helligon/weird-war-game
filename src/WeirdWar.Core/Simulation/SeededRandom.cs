using System;

namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// The single seeded random source of the simulation (TEC-008, TEC-012). SplitMix64: its whole
    /// position is one 64-bit value, so it is part of the game state and hashed with it. Internal: only
    /// commands (inside the core) may draw, so every draw is logged and replayable (DLV-013).
    /// </summary>
    internal sealed class SeededRandom
    {
        public SeededRandom(ulong seed) { Position = seed; }

        /// <summary>The generator's current position. Two sources with the same position give the same sequence.</summary>
        public ulong Position { get; private set; }

        internal ulong NextUInt64()
        {
            unchecked
            {
                Position += 0x9E3779B97F4A7C15UL;
                ulong z = Position;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>A value in [0, maxExclusive), without modulo bias (rejection sampling).</summary>
        internal int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            ulong max = (ulong)maxExclusive;
            ulong excess = (ulong.MaxValue % max + 1) % max; // 2^64 mod max
            ulong r;
            do { r = NextUInt64(); } while (r > ulong.MaxValue - excess);
            return (int)(r % max);
        }
    }
}
