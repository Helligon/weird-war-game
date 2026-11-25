namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// A small deterministic random source (SplitMix64) so fire is repeatable (TEC-008): the same seed always gives
    /// the same sequence, on every platform. Never seeded from the clock.
    /// </summary>
    public sealed class SeededRandom
    {
        private ulong _state;

        public SeededRandom(ulong seed) { _state = seed; }

        /// <summary>The next raw 64-bit value of the sequence.</summary>
        public ulong NextUInt64()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                var z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>The next number in [0, 1): the top 53 bits of the next raw value.</summary>
        public double NextDouble() => (NextUInt64() >> 11) * (1.0 / 9007199254740992.0);
    }
}
