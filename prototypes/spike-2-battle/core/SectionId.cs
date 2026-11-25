using System;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>Names one Section. Commands refer to Sections only, never to Units (HIE-007).</summary>
    public readonly struct SectionId : IEquatable<SectionId>
    {
        public int Value { get; }

        public SectionId(int value) { Value = value; }

        public bool Equals(SectionId other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is SectionId o && Equals(o);
        public override int GetHashCode() => Value;
        public override string ToString() => "Section " + Value;
    }
}
