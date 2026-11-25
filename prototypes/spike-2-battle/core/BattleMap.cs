using System;
using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// The greybox battle map (BMP-004, BMP-005, BMP-010): a rectangle with a west edge and an east edge, plus cover objects.
    /// Read-only once loaded. Cover positions are listed here for the Section logic to use.
    /// </summary>
    public sealed class BattleMap
    {
        public double Width { get; }
        public double Height { get; }
        public MapEdge WestEdge { get; }
        public MapEdge EastEdge { get; }
        public IReadOnlyList<CoverObject> CoverObjects { get; }

        public BattleMap(double width, double height, IReadOnlyList<CoverObject> cover)
        {
            Width = width;
            Height = height;
            WestEdge = new MapEdge(MapEdgeSide.West, 0);
            EastEdge = new MapEdge(MapEdgeSide.East, width);
            CoverObjects = cover;
        }

        /// <summary>Width in metres of the strip along each edge that is kept clear of cover (and drawn in the edge colour).</summary>
        public const double EdgeStripWidth = 3;

        /// <summary>BMP-004: the map edge that belongs to a side. Spike stand-in: Player is West, Enemy is East.</summary>
        public MapEdge EdgeOf(Side side)
        {
            switch (side)
            {
                case Side.Player: return WestEdge;
                case Side.Enemy: return EastEdge;
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        /// <summary>The distance between the two edges, in metres.</summary>
        public double EdgeDistance => EastEdge.X - WestEdge.X;

        /// <summary>BMP-010: the time in seconds for a Unit at the given speed (metres per second) to cross from one edge to the other.</summary>
        public double CrossingSeconds(double speedMetresPerSecond)
        {
            if (speedMetresPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(speedMetresPerSecond), "Speed must be above zero.");
            return EdgeDistance / speedMetresPerSecond;
        }

        /// <summary>The cover object that contains the point, or null when the point is in open ground.</summary>
        public CoverObject? CoverAt(MapPoint p)
        {
            foreach (var c in CoverObjects)
            {
                if (c.Contains(p)) return c;
            }
            return null;
        }
    }
}
