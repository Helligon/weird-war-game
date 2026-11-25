namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>A piece of cover a Section can use (BMP-005): a round area with a centre and radius, in metres.</summary>
    public sealed class CoverObject
    {
        public string Id { get; }
        public MapPoint Position { get; }
        public double Radius { get; }

        public CoverObject(string id, MapPoint position, double radius)
        {
            Id = id;
            Position = position;
            Radius = radius;
        }

        /// <summary>True when the point is inside this cover object.</summary>
        public bool Contains(MapPoint p)
        {
            var dx = p.X - Position.X;
            var dy = p.Y - Position.Y;
            return dx * dx + dy * dy <= Radius * Radius;
        }
    }
}
