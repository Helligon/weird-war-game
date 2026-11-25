namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>Which side of the map an edge is on.</summary>
    public enum MapEdgeSide
    {
        West,
        East,
    }

    /// <summary>
    /// One side's own map edge (BMP-004): the edge that side's routing Units flee towards.
    /// It is a straight line the full height of the map, at a fixed X.
    /// </summary>
    public sealed class MapEdge
    {
        public MapEdgeSide Side { get; }
        public double X { get; }

        public MapEdge(MapEdgeSide side, double x) { Side = side; X = x; }
    }
}
