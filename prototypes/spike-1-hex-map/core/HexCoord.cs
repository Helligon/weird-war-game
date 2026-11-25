using System;

namespace WeirdWar.Spike1.Core
{
    /// <summary>
    /// A hex on the campaign map (MAP-017), in axial coordinates (Q, R) with S = -Q - R.
    /// Used for all hex maths; convert to <see cref="OffsetCoord"/> for storage and display.
    /// Integer-only, so results are deterministic on every platform (TEC-008).
    /// </summary>
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        // Flat-top axial directions, in a fixed order: up, up-right, down-right, down, down-left, up-left.
        private static readonly HexCoord[] Directions =
        {
            new HexCoord(0, -1),
            new HexCoord(1, -1),
            new HexCoord(1, 0),
            new HexCoord(0, 1),
            new HexCoord(-1, 1),
            new HexCoord(-1, 0),
        };

        public int Q { get; }
        public int R { get; }
        public int S => -Q - R;

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        /// <summary>The 6 adjacent hexes, in a fixed order. Not clipped to any map bounds.</summary>
        public HexCoord[] Neighbours()
        {
            var result = new HexCoord[Directions.Length];
            for (var i = 0; i < Directions.Length; i++)
            {
                result[i] = new HexCoord(Q + Directions[i].Q, R + Directions[i].R);
            }
            return result;
        }

        /// <summary>Number of hex-to-hex steps on the shortest path between the two hexes.</summary>
        public int DistanceTo(HexCoord other)
        {
            var dq = Q - other.Q;
            var dr = R - other.R;
            var ds = S - other.S;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(ds)) / 2;
        }

        /// <summary>Converts an odd-q (column, row) position to axial.</summary>
        public static HexCoord FromOffset(OffsetCoord offset)
        {
            var q = offset.Column;
            var r = offset.Row - (offset.Column - (offset.Column & 1)) / 2;
            return new HexCoord(q, r);
        }

        /// <summary>Converts this hex to an odd-q (column, row) position.</summary>
        public OffsetCoord ToOffset()
        {
            var column = Q;
            var row = R + (Q - (Q & 1)) / 2;
            return new OffsetCoord(column, row);
        }

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object? obj) => obj is HexCoord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Q, R);
        public static bool operator ==(HexCoord a, HexCoord b) => a.Equals(b);
        public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);
        public override string ToString() => $"(q {Q}, r {R})";
    }
}
