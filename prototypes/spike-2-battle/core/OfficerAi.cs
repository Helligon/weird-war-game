using System;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// What the player's Officer does while under AI control (DAI-005, CMD-010): it Protects the nearest friendly Section. It stays
    /// beside or in front of that Section (the front is at most P-DAI-02 from its centre) and fires at the nearest enemy Section it can reach
    /// from where it stands, never walking away from the Section to do so.
    /// It reads only core state: no clock, no view flag. Its one random roll (the hit, P-DAI-04, DEC-102) comes from the battle's own
    /// seeded source in <see cref="BattleState.FireOfficerRifle"/>, so the same logged commands replay the same battle (TEC-008).
    /// Whether the Officer is under AI is core state, changed only by <see cref="OfficerControlCommand"/>.
    /// </summary>
    internal static class OfficerAi
    {
        private const double Arrived = 0.25;

        internal static void Run(BattleState state, Officer self, double seconds)
        {
            self.FireCooldown = Math.Max(0, self.FireCooldown - seconds);
            var guard = NearestSection(state, self.Side, self.Position, true);
            if (guard == null) { self.MoveDirection = new MapPoint(0, 0); return; }

            // In front: towards the enemy nearest the Section, at the protect distance. With no enemy left, beside it (on it).
            var station = guard.Position;
            var threat = NearestSection(state, self.Side, guard.Position, false);
            if (threat != null)
            {
                var d = Distance(guard.Position, threat.Position);
                if (d > 0)
                {
                    var lead = Math.Min(state.Parameters.OfficerProtectDistance, d);
                    station = new MapPoint(guard.Position.X + (threat.Position.X - guard.Position.X) / d * lead,
                        guard.Position.Y + (threat.Position.Y - guard.Position.Y) / d * lead);
                }
            }
            Walk(state, self, station, seconds);

            // Engage the nearest enemy in rifle reach of where the Officer stands now (it does not leave the Section's side to do it).
            var target = NearestSection(state, self.Side, self.Position, false);
            if (target == null || Distance(self.Position, target.Position) > state.Stats.OfficerRifleRange) return;
            self.AimRadians = Math.Atan2(target.Position.Y - self.Position.Y, target.Position.X - self.Position.X);
            if (self.FireCooldown > 0) return;
            state.FireOfficerRifle(self);
            self.FireCooldown = state.Parameters.OfficerFireInterval;
        }

        private static void Walk(BattleState state, Officer self, MapPoint to, double seconds)
        {
            var dx = to.X - self.Position.X;
            var dy = to.Y - self.Position.Y;
            var d = Math.Sqrt(dx * dx + dy * dy);
            if (d <= Arrived) { self.MoveDirection = new MapPoint(0, 0); return; }
            // Cut the direction so one step lands on the point instead of overshooting it.
            var reach = state.Stats.OfficerSpeed * seconds;
            var scale = reach > 0 ? Math.Min(1.0, d / reach) : 1.0;
            self.MoveDirection = new MapPoint(dx / d * scale, dy / d * scale);
        }

        /// <summary>The nearest living Section of the side (own) or of the other side (own = false) to the point. Ties go to the lower SectionId.</summary>
        private static Section? NearestSection(BattleState state, Side side, MapPoint from, bool own)
        {
            Section? best = null;
            var bestDistance = double.MaxValue;
            foreach (var s in state.Sections)
            {
                if (s.IsDestroyed || (s.Side == side) != own) continue;
                var d = Distance(from, s.Position);
                if (d < bestDistance) { best = s; bestDistance = d; }
            }
            return best;
        }

        private static double Distance(MapPoint a, MapPoint b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
