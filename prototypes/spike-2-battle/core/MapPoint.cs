namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>A position on the battle map, in metres. X runs from the west edge to the east edge, Y from south to north.</summary>
    public readonly struct MapPoint
    {
        public readonly double X;
        public readonly double Y;

        public MapPoint(double x, double y) { X = x; Y = y; }
    }
}
