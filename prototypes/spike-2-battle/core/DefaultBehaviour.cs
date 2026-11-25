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

        internal static void Run(BattleState state, Section self, double seconds)
        {
            self.VolleyCooldown = Math.Max(0, self.VolleyCooldown - seconds);
            var moved = false;

            if (!self.IsHolding)
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

            // DAI-004, DAI-006: fire at the chosen target, once per volley interval. Chosen after moving.
            if (self.VolleyCooldown > 0) return;
            var target = ChooseTarget(state, self);
            if (!target.HasValue) return;
            var result = target.Value.Section != null
                ? state.Fire(self.Id, target.Value.Section.Id, moved)
                : state.FireAtOfficer(self.Id, target.Value.Officer!, moved);
            if (result.Outcome == FireOutcome.Fired) self.VolleyCooldown = state.Stats.RiflemenVolleyInterval;
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
                if (d > state.Stats.CoverSearchRange || Distance(c.Position, enemy) > own) continue;
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
            var step = Math.Min(d, state.Stats.RiflemenSpeed * seconds);
            if (step <= 0) return false;
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
