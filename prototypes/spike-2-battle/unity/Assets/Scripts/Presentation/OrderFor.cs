using System;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Presentation
{
    /// <summary>
    /// Builds Section commands for the temporary order buttons (SP2-41 replaces them with the tactical view). It only reads the
    /// battle and returns plain commands; it changes nothing (DLV-013). Waypoints are whole metres (Q east, R north).
    /// </summary>
    public static class OrderFor
    {
        /// <summary>A Move with one waypoint, an offset from the Section's last queued waypoint (or from where it stands), kept on the map.</summary>
        public static IBattleCommand MoveBy(BattleState state, SectionId id, int dx, int dy)
        {
            var from = LastPoint(state.Find(id));
            return new MoveCommand(id, new[] { OnMap(state, from.X + dx, from.Y + dy) });
        }

        /// <summary>A Move with one waypoint just west of the nearest cover object, so it counts as next to cover (CMD-005).</summary>
        public static IBattleCommand MoveBesideCover(BattleState state, SectionId id)
        {
            var from = LastPoint(state.Find(id));
            CoverObject best = null;
            var bestDistance = double.MaxValue;
            foreach (var c in state.Map.CoverObjects)
            {
                var d = Math.Abs(c.Position.X - from.X) + Math.Abs(c.Position.Y - from.Y);
                if (d < bestDistance) { best = c; bestDistance = d; }
            }
            if (best == null) return new MoveCommand(id, Array.Empty<Waypoint>()); // the core rejects it and says why
            return new MoveCommand(id, new[] { OnMap(state, best.Position.X - best.Radius - 1, best.Position.Y) });
        }

        private static MapPoint LastPoint(Section s) =>
            s.Waypoints.Count > 0 ? s.Waypoints[s.Waypoints.Count - 1] : s.Position;

        private static Waypoint OnMap(BattleState state, double x, double y) =>
            new Waypoint((int)Math.Round(Math.Min(state.Map.Width, Math.Max(0, x))), (int)Math.Round(Math.Min(state.Map.Height, Math.Max(0, y))));
    }
}
