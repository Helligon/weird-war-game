using System;
using System.Collections.Generic;
using System.Text;

namespace WeirdWar.Core.Maps
{
    /// <summary>
    /// Terrain of one hex (MED-006, TRN-024). Open is the only passable type. Terrain costs for other
    /// passable types (SUP-011, TRN-025, TRN-026) are not decided and not built.
    /// </summary>
    public enum TerrainType
    {
        Open = 0,
        Mountain = 1,
        Sea = 2,
        River = 3,
    }

    /// <summary>
    /// An immutable hex grid with terrain per hex (MAP-017). Built only by <see cref="MapImporter"/>
    /// (MAP-018: maps are fixed and hand-made, never generated).
    /// </summary>
    public sealed class HexMap
    {
        private readonly TerrainType[] _cells;

        public int Width { get; }
        public int Height { get; }

        internal HexMap(int width, int height, TerrainType[] cells)
        {
            Width = width;
            Height = height;
            _cells = cells;
        }

        public bool Contains(int column, int row)
            => column >= 0 && column < Width && row >= 0 && row < Height;

        public bool Contains(HexCoord hex)
        {
            var (c, r) = hex.ToOffset();
            return Contains(c, r);
        }

        public TerrainType TerrainAt(int column, int row)
        {
            if (!Contains(column, row)) throw new ArgumentOutOfRangeException(column < 0 || column >= Width ? nameof(column) : nameof(row), "Hex is outside the map");
            return _cells[row * Width + column];
        }

        public TerrainType TerrainAt(HexCoord hex)
        {
            var (c, r) = hex.ToOffset();
            return TerrainAt(c, r);
        }

        /// <summary>Neighbours that are on the map: 6 inside the map, fewer at the edge (MAP-017).</summary>
        public List<HexCoord> Neighbours(HexCoord hex)
        {
            var result = new List<HexCoord>(6);
            foreach (var n in hex.Neighbours())
                if (Contains(n)) result.Add(n);
            return result;
        }

        /// <summary>
        /// True when the terrain lets ground Brigades enter the hex: on the map and open (TRN-024).
        /// Terrain only: enemy occupation (TRN-023) and bridges are not included and are checked elsewhere.
        /// </summary>
        public bool IsPassable(HexCoord hex) => Contains(hex) && TerrainAt(hex) == TerrainType.Open;

        /// <summary>
        /// Terrain factor for entering the hex: 1 on every open hex, null when impassable (MED-006: all open
        /// hexes are equal). This is a multiplier, not an SP amount: the SP per hex comes from the Brigade
        /// (SUP-021).
        /// </summary>
        public int? TerrainCostFactor(HexCoord hex) => IsPassable(hex) ? 1 : (int?)null;

        /// <summary>Stable text form (one letter per hex, row by row) for hashing and tests (TEC-008).</summary>
        public string CanonicalText()
        {
            var sb = new StringBuilder();
            sb.Append(Width).Append('x').Append(Height);
            for (var r = 0; r < Height; r++)
            {
                sb.Append('\n');
                for (var c = 0; c < Width; c++)
                    sb.Append(_cells[r * Width + c] switch
                    {
                        TerrainType.Open => '.',
                        TerrainType.Mountain => 'M',
                        TerrainType.Sea => 'S',
                        TerrainType.River => 'R',
                        var other => throw new InvalidOperationException("No canonical letter for terrain " + other),
                    });
            }
            return sb.ToString();
        }
    }
}
