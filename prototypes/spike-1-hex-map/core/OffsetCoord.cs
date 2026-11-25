using System;

namespace WeirdWar.Spike1.Core
{
    /// <summary>
    /// A map position as (column, row), the storage and display form of a hex (MAP-017).
    /// Layout is flat-top, odd-q: odd columns sit half a hex lower than even columns.
    /// </summary>
    public readonly struct OffsetCoord : IEquatable<OffsetCoord>
    {
        public int Column { get; }
        public int Row { get; }

        public OffsetCoord(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public bool Equals(OffsetCoord other) => Column == other.Column && Row == other.Row;
        public override bool Equals(object? obj) => obj is OffsetCoord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Column, Row);
        public static bool operator ==(OffsetCoord a, OffsetCoord b) => a.Equals(b);
        public static bool operator !=(OffsetCoord a, OffsetCoord b) => !a.Equals(b);
        public override string ToString() => $"(col {Column}, row {Row})";
    }
}
