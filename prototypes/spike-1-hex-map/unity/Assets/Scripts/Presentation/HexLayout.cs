using UnityEngine;
using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Presentation
{
    /// <summary>
    /// Where a hex sits in the world (MAP-017). Presentation only: it converts the core's coordinates and
    /// holds no rules. Flat-top, odd-q, matching the core: odd columns sit half a hex lower, and row 0 is at the top.
    /// The hex "size" is the distance from its centre to a corner.
    /// </summary>
    public static class HexLayout
    {
        private static readonly float Sqrt3 = Mathf.Sqrt(3f);

        public static Vector3 HexToWorld(HexCoord hex, float size = 1f)
        {
            var x = size * 1.5f * hex.Q;
            var y = -size * Sqrt3 * (hex.R + hex.Q / 2f);
            return new Vector3(x, y, 0f);
        }

        public static Vector3 OffsetToWorld(int column, int row, float size = 1f)
        {
            return HexToWorld(HexCoord.FromOffset(new OffsetCoord(column, row)), size);
        }

        /// <summary>Corner i (0 to 5) of a flat-top hex, as an offset from its centre.</summary>
        public static Vector3 Corner(int i, float size = 1f)
        {
            var angle = Mathf.Deg2Rad * 60f * i;
            return new Vector3(size * Mathf.Cos(angle), size * Mathf.Sin(angle), 0f);
        }
    }
}
