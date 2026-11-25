using System;
using System.Collections.Generic;

namespace WeirdWar.Spike1.Core
{
    /// <summary>
    /// A fixed, hand-made hex map (MAP-017, MAP-018). Every cell has a terrain type.
    /// Immutable once built. Cells are addressed by <see cref="HexCoord"/>; storage is odd-q (column, row).
    /// </summary>
    public sealed class HexMap : IEquatable<HexMap>
    {
        private readonly TerrainType[] _cells; // index = row * Width + column

        public int Width { get; }
        public int Height { get; }

        internal HexMap(int width, int height, TerrainType[] cells)
        {
            Width = width;
            Height = height;
            _cells = cells;
        }

        /// <summary>True if the hex is on the map.</summary>
        public bool Contains(HexCoord hex)
        {
            var o = hex.ToOffset();
            return o.Column >= 0 && o.Column < Width && o.Row >= 0 && o.Row < Height;
        }

        /// <summary>Terrain at a hex on the map. Throws for a hex outside it.</summary>
        public TerrainType TerrainAt(HexCoord hex)
        {
            if (!Contains(hex))
            {
                throw new ArgumentOutOfRangeException(nameof(hex), hex + " is outside the map");
            }
            var o = hex.ToOffset();
            return _cells[o.Row * Width + o.Column];
        }

        /// <summary>Adjacent hexes that are on the map (MAP-017), in the fixed order of <see cref="HexCoord.Neighbours"/>.</summary>
        public List<HexCoord> Neighbours(HexCoord hex)
        {
            var result = new List<HexCoord>(6);
            foreach (var n in hex.Neighbours())
            {
                if (Contains(n))
                {
                    result.Add(n);
                }
            }
            return result;
        }

        /// <summary>
        /// Terrain passability for ground Brigades (TRN-023, TRN-024). Off the map, mountain, sea and river
        /// are impassable. Enemy occupation is a separate rule and not checked here.
        /// </summary>
        public bool IsPassable(HexCoord hex) => Contains(hex) && TerrainAt(hex) == TerrainType.Plain;

        public bool Equals(HexMap? other)
        {
            if (other is null || Width != other.Width || Height != other.Height)
            {
                return false;
            }
            for (var i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] != other._cells[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as HexMap);

        public override int GetHashCode()
        {
            var h = HashCode.Combine(Width, Height);
            foreach (var c in _cells)
            {
                h = HashCode.Combine(h, (int)c);
            }
            return h;
        }
    }
}
