using System;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>The thing a Section has chosen to shoot: an enemy Section or an enemy Officer.</summary>
    public readonly struct TargetChoice
    {
        public Section? Section { get; }
        public Officer? Officer { get; }
        public MapPoint Position => Section != null ? Section.Position : Officer!.Position;

        public TargetChoice(Section section) { Section = section; Officer = null; }
        public TargetChoice(Officer officer) { Section = null; Officer = officer; }
    }

    /// <summary>
    /// What a Section does with no orders (DAI-002: deliberately simple). Only these behaviours:
    /// engage (DAI-001), take nearby cover (DAI-001), target the nearest enemy in range, preferring an attacker (DAI-006),
    /// and hold and fire after Stop (DAI-004). Nothing else.
    /// </summary>
    public static class DefaultBehaviour
    {
        /// <summary>
        /// DAI-006 (DEC-096): the enemy that has fired at this Section and is in range, else the nearest enemy in range.
        /// Enemy Officers count. Ties go to Sections (by SectionId), then Officers (in list order). Null when none is in range.
        /// </summary>
        public static TargetChoice? ChooseTarget(BattleState state, Section self)
        {
            TargetChoice? best = null;
            var bestDistance = double.MaxValue;
            foreach (var other in state.Sections)
            {
                if (other.Side == self.Side || other.IsDestroyed) continue;
                if (!state.InRange(self.Position, other.Position)) continue;
                var d = Distance(self.Position, other.Position);
                if (self.LastAttacker?.Section is SectionId attackerId && other.Id.Equals(attackerId)) return new TargetChoice(other);
                if (d < bestDistance) { best = new TargetChoice(other); bestDistance = d; }
            }
            foreach (var officer in state.Officers)
            {
                if (officer.Side == self.Side || officer.IsDestroyed) continue;
                if (!state.InRange(self.Position, officer.Position)) continue;
                var d = Distance(self.Position, officer.Position);
                if (self.LastAttacker?.Officer == officer) return new TargetChoice(officer);
                if (d < bestDistance) { best = new TargetChoice(officer); bestDistance = d; }
            }
            return best;
        }

        /// <summary>
        /// One step of a Section's life. With queued waypoints it carries out its orders (CMD-005, CMD-008: moving first). With none
        /// it holds if told to (CMD-006, CMD-011, DAI-004), else it follows default behaviour (CMD-012, DAI-001), engaging an
        /// ordered Target if it has one (CMD-008). It fires in every case, once per volley interval (CMD-019, DAI-004, DAI-006).
        /// </summary>
        internal static void Run(BattleState state, Section self, double seconds)
        {
            self.VolleyCooldown = Math.Max(0, self.VolleyCooldown - seconds);
            var moved = false;
            var ordered = OrderedTarget(state, self);

            if (self.QueuedWaypoints.Count > 0)
            {
                moved = FollowWaypoint(state, self, seconds);
            }
            else if (!self.IsHolding)
            {
                if (ordered != null)
                {
                    // CMD-008: attack the ordered Section: close to firing range, then take nearby cover as default behaviour does.
                    if (!state.InRange(self.Position, ordered.Position)) moved = StepToward(state, self, ordered.Position, seconds);
                    else if (state.Map.CoverAt(self.Position) == null)
                    {
                        var cover = NearbyCover(state, self, ordered.Position);
                        if (cover != null) moved = StepToward(state, self, cover.Position, seconds);
                    }
                }
                else
                {
                    // DAI-001: walk straight at the nearest enemy. Once one is in range, step into nearby cover (if any) and stay.
                    var chosen = ChooseTarget(state, self);
                    if (chosen == null)
                    {
                        var enemy = NearestEnemy(state, self);
                        if (enemy.HasValue) moved = StepToward(state, self, enemy.Value.Position, seconds);
                    }
                    else if (state.Map.CoverAt(self.Position) == null)
                    {
                        var cover = NearbyCover(state, self, chosen.Value.Position);
                        if (cover != null) moved = StepToward(state, self, cover.Position, seconds);
                    }
                }
            }

            // DAI-004, DAI-006, CMD-008: fire at the ordered Target if it is in range, else the default choice. Chosen after moving.
            if (self.VolleyCooldown > 0) return;
            TargetChoice? target = null;
            if (ordered != null && state.InRange(self.Position, ordered.Position)) target = new TargetChoice(ordered);
            if (!target.HasValue) target = ChooseTarget(state, self);
            if (!target.HasValue) return;
            var result = target.Value.Section != null
                ? state.Fire(self.Id, target.Value.Section.Id, moved)
                : state.FireAtOfficer(self.Id, target.Value.Officer!, moved);
            if (result.Outcome == FireOutcome.Fired) self.VolleyCooldown = state.Stats.RiflemenVolleyInterval;
        }

        /// <summary>CMD-008: the living enemy Section this one was ordered to attack. A destroyed target ends the order.</summary>
        private static Section? OrderedTarget(BattleState state, Section self)
        {
            if (!(self.OrderedTarget is SectionId id)) return null;
            var target = state.Find(id);
            if (target == null || target.IsDestroyed) { self.OrderedTarget = null; return null; }
            return target;
        }

        /// <summary>
        /// CMD-005: walks to the first queued waypoint. On arrival at one next to cover (within the adjacency distance of a cover
        /// object's edge) it steps into that cover before the waypoint counts as done. The last waypoint done: hold if a Stop ends the
        /// orders (CMD-006, CMD-011), else the Section is back on default behaviour (CMD-012). True when it moved.
        /// </summary>
        private static bool FollowWaypoint(BattleState state, Section self, double seconds)
        {
            var wp = self.QueuedWaypoints[0];
            var moved = false;
            var cover = wp.SeekCover ? CoverNextTo(state, wp.Point) : null;
            // Only after reaching the waypoint (TakingCover) does being inside that cover finish it. A Section that starts inside
            // the cover still walks to a waypoint on the far side of it.
            var inThatCover = self.TakingCover && cover != null && cover.Contains(self.Position);
            if (!inThatCover)
            {
                if (!self.TakingCover && Distance(self.Position, wp.Point) > 0)
                {
                    moved = StepToward(state, self, wp.Point, seconds);
                    if (Distance(self.Position, wp.Point) > 0) return moved;
                }
                if (cover != null)
                {
                    // Arrived: step towards the cover centre until inside it, and the waypoint is done.
                    self.TakingCover = true;
                    return StepToward(state, self, cover.Position, seconds) || moved;
                }
            }
            self.DequeueWaypoint();
            if (self.QueuedWaypoints.Count == 0 && self.StopAtEnd)
            {
                self.IsHolding = true;
                self.StopAtEnd = false;
            }
            return moved;
        }

        /// <summary>The nearest cover object whose edge is within the adjacency distance of the point (CMD-005), or null.</summary>
        private static CoverObject? CoverNextTo(BattleState state, MapPoint point)
        {
            CoverObject? best = null;
            var bestDistance = double.MaxValue;
            foreach (var c in state.Map.CoverObjects)
            {
                var gap = Distance(point, c.Position) - c.Radius;
                if (gap > state.Stats.CoverAdjacencyDistance) continue;
                if (gap < bestDistance) { best = c; bestDistance = gap; }
            }
            return best;
        }

        private static TargetChoice? NearestEnemy(BattleState state, Section self)
        {
            TargetChoice? best = null;
            var bestDistance = double.MaxValue;
            foreach (var other in state.Sections)
            {
                if (other.Side == self.Side || other.IsDestroyed) continue;
                var d = Distance(self.Position, other.Position);
                if (d < bestDistance) { best = new TargetChoice(other); bestDistance = d; }
            }
            foreach (var officer in state.Officers)
            {
                if (officer.Side == self.Side || officer.IsDestroyed) continue;
                var d = Distance(self.Position, officer.Position);
                if (d < bestDistance) { best = new TargetChoice(officer); bestDistance = d; }
            }
            return best;
        }

        /// <summary>
        /// The nearest cover within the search range that is no farther from the
        /// target than the Section is (so it never backs away to find cover).
        /// </summary>
        private static CoverObject? NearbyCover(BattleState state, Section self, MapPoint enemy)
        {
            CoverObject? best = null;
            var bestDistance = double.MaxValue;
            var own = Distance(self.Position, enemy);
            foreach (var c in state.Map.CoverObjects)
            {
                var d = Distance(self.Position, c.Position);
                if (d > state.Parameters.CoverSearchRange || Distance(c.Position, enemy) > own) continue;
                if (d < bestDistance) { best = c; bestDistance = d; }
            }
            return best;
        }

        /// <summary>Walks at Riflemen speed toward the point, without overshooting. True when it moved.</summary>
        private static bool StepToward(BattleState state, Section self, MapPoint to, double seconds)
        {
            var dx = to.X - self.Position.X;
            var dy = to.Y - self.Position.Y;
            var d = Math.Sqrt(dx * dx + dy * dy);
            var maxStep = state.Stats.RiflemenSpeed * seconds;
            if (d <= 0 || maxStep <= 0) return false;
            // Within rounding of the point: arrive exactly, so waypoints are reached on the step they should be.
            if (d <= maxStep + 1e-9) { self.MoveTo(to); return true; }
            var step = maxStep;
            self.MoveTo(new MapPoint(self.Position.X + dx / d * step, self.Position.Y + dy / d * step));
            return true;
        }

        private static double Distance(MapPoint a, MapPoint b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
