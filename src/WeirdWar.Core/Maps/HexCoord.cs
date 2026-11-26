using System;

namespace WeirdWar.Core.Maps
{
    /// <summary>
    /// A hex position in axial coordinates (MAP-017). The map file uses offset (column, row) positions;
    /// convert with <see cref="FromOffset"/> and <see cref="ToOffset"/>. See MapFormat.md for the layout.
    /// </summary>
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        /// <summary>The six neighbour steps (axial q, r), in a fixed order.</summary>
        private static readonly (int Q, int R)[] Directions =
        {
            (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1),
        };

        public int Q { get; }
        public int R { get; }

        public HexCoord(int q, int r) { Q = q; R = r; }

        /// <summary>Flat-top, odd-q layout: odd columns sit half a hex lower.</summary>
        public static HexCoord FromOffset(int column, int row)
            => new HexCoord(column, row - (column - (column & 1)) / 2);

        public (int Column, int Row) ToOffset()
            => (Q, R + (Q - (Q & 1)) / 2);

        /// <summary>The six adjacent hexes, whether or not they are on a map.</summary>
        public HexCoord[] Neighbours()
        {
            var result = new HexCoord[Directions.Length];
            for (var i = 0; i < result.Length; i++)
                result[i] = new HexCoord(Q + Directions[i].Q, R + Directions[i].R);
            return result;
        }

        /// <summary>Number of steps on the shortest path between two hexes.</summary>
        public int DistanceTo(HexCoord other)
        {
            var dq = Q - other.Q;
            var dr = R - other.R;
            var ds = -dq - dr;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(ds)) / 2;
        }

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object? obj) => obj is HexCoord h && Equals(h);
        public override int GetHashCode() => unchecked(Q * 397 ^ R);
        public override string ToString() => "(" + Q + "," + R + ")";
    }
}
