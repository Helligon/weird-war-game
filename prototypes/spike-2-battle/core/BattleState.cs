using System;
using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>Why a volley did or did not happen.</summary>
    public enum FireOutcome
    {
        Fired = 1,
        OutOfRange = 2,
        ShooterDestroyed = 3,
        TargetDestroyed = 4,
    }

    /// <summary>What one volley did.</summary>
    public readonly struct FireResult
    {
        public FireOutcome Outcome { get; }
        /// <summary>True when the volley was fired (the target was in range and both Sections were alive).</summary>
        public bool InRange => Outcome == FireOutcome.Fired;
        public int Shots { get; }
        public int Hits { get; }
        public int DamageDealt { get; }
        /// <summary>The hit chance used for each shot, after any moving penalty (CMD-019).</summary>
        public double HitChance { get; }

        public FireResult(FireOutcome outcome, int shots, int hits, int damageDealt, double hitChance)
        {
            Outcome = outcome; Shots = shots; Hits = hits; DamageDealt = damageDealt; HitChance = hitChance;
        }
    }

    /// <summary>
    /// The battle state for the spike: Sections and Officers. Plain C#, no Unity (DLV-012). Implements
    /// <see cref="IBattleView"/> so commands can be validated against it. Fire is placeholder: its damage and range are open.
    /// </summary>
    public sealed class BattleState : IBattleView
    {
        public static readonly SectionId PlayerSectionId = new SectionId(1);
        public static readonly SectionId EnemySectionId = new SectionId(2);

        private readonly SortedDictionary<int, Section> _sections = new SortedDictionary<int, Section>();
        private readonly List<Officer> _officers = new List<Officer>();
        private readonly BattleMap _map;
        private readonly UnitStats _stats;
        private readonly BattleParameters _parameters;
        private readonly SeededRandom _random;

        /// <summary>All Sections, in SectionId order (stable, TEC-008).</summary>
        public IEnumerable<Section> Sections => _sections.Values;
        public IReadOnlyList<Officer> Officers => _officers;
        public BattleMap Map => _map;
        public UnitStats Stats => _stats;

        private readonly List<LoggedCommand> _commandLog = new List<LoggedCommand>();

        /// <summary>The number of <see cref="Step"/> calls so far. A command applied now is logged with this number.</summary>
        public int StepCount { get; private set; }

        /// <summary>PCH-009, TEC-008: every accepted Officer command with the step it was applied at, in order.</summary>
        public IReadOnlyList<LoggedCommand> CommandLog => _commandLog;

        private BattleState(BattleMap map, UnitStats stats, BattleParameters parameters, ulong seed)
        {
            _map = map;
            _random = new SeededRandom(seed);
            _stats = stats;
            _parameters = parameters;
        }

        /// <summary>
        /// ROS-001: both sides field a Riflemen Section, sized from the stats file (P-HIE-01, P-HIE-02). Spike stand-in:
        /// the player has 1 Section and 1 Officer, the enemy 1 Section and no Officer. Each stands just inside its own
        /// edge strip (BMP-004), at the middle of the map's height. The battle owns one random source, seeded here (TEC-008).
        /// </summary>
        public static BattleState SetUp(BattleMap map, UnitStats stats, BattleParameters parameters, ulong seed)
        {
            var state = new BattleState(map, stats, parameters, seed);
            var midY = map.Height / 2;
            var inset = BattleMap.EdgeStripWidth * 2;
            var playerPos = new MapPoint(map.EdgeOf(Side.Player).X + inset, midY);
            var enemyPos = new MapPoint(map.EdgeOf(Side.Enemy).X - inset, midY);
            state.Add(new Section(PlayerSectionId, Side.Player, UnitType.Riflemen, stats.RiflemenMaxUnits, stats.RiflemenHealthPerUnit, playerPos));
            state.Add(new Section(EnemySectionId, Side.Enemy, UnitType.Riflemen, stats.RiflemenMaxUnits, stats.RiflemenHealthPerUnit, enemyPos));
            state._officers.Add(new Officer(Side.Player, stats.OfficerHealth, new MapPoint(playerPos.X - 1, midY)));
            return state;
        }

        /// <summary>Adds a Section. Used by set-up and by tests that need a particular layout.</summary>
        public void Add(Section section) { _sections.Add(section.Id.Value, section); }

        /// <summary>Adds an Officer. Used by tests that need a particular layout (the enemy has none at set-up).</summary>
        internal void AddOfficer(Officer officer) { _officers.Add(officer); }

        public Section? Find(SectionId id) => _sections.TryGetValue(id.Value, out var s) ? s : null;

        /// <summary>Queued waypoints are not tracked yet (movement is a later Story), so validation sees none.</summary>
        public bool TryGetSection(SectionId id, out SectionInfo info)
        {
            if (_sections.TryGetValue(id.Value, out var s)) { info = new SectionInfo(s.Side, 0); return true; }
            info = default;
            return false;
        }

        /// <summary>The hit chance of one Riflemen shot, moving or standing still (CMD-019).</summary>
        public double HitChance(bool moving) =>
            _stats.RiflemenAccuracy * (moving ? 1.0 - _parameters.MovingAccuracyPenalty : 1.0);

        /// <summary>
        /// Placeholder ranged fire: each of the shooter's Units fires one shot; each hit takes damage off the target's
        /// health. Both Sections must be in this battle and on opposite sides (else ArgumentException). Out of range, or a
        /// destroyed shooter or target, does nothing and says why. Rolls come only from the battle's own seeded source.
        /// <paramref name="moving"/> says whether the shooter fires on the move (CMD-019); movement itself is a later Story.
        /// </summary>
        public FireResult Fire(SectionId shooterId, SectionId targetId, bool moving = false)
        {
            var shooter = Find(shooterId) ?? throw new ArgumentException(shooterId + " does not exist.");
            var target = Find(targetId) ?? throw new ArgumentException(targetId + " does not exist.");
            if (shooter.Side == target.Side) throw new ArgumentException(shooterId + " cannot fire at its own side (CMD-008).");
            return Volley(shooter, target.Position, target.IsDestroyed, moving, target.TakeDamage, () => target.Health, target);
        }

        /// <summary>DAI-006 (DEC-096): the same placeholder volley, at an enemy Officer. Officers do not fire back in this spike.</summary>
        public FireResult FireAtOfficer(SectionId shooterId, Officer target, bool moving = false)
        {
            var shooter = Find(shooterId) ?? throw new ArgumentException(shooterId + " does not exist.");
            if (!_officers.Contains(target)) throw new ArgumentException("That Officer is not in this battle.");
            if (shooter.Side == target.Side) throw new ArgumentException(shooterId + " cannot fire at its own side (CMD-008).");
            return Volley(shooter, target.Position, target.IsDestroyed, moving, target.TakeDamage, () => target.Health, null);
        }

        private FireResult Volley(Section shooter, MapPoint targetPosition, bool targetDestroyed, bool moving,
            Action<int> damage, Func<int> health, Section? targetSection)
        {
            var chance = HitChance(moving);
            if (shooter.IsDestroyed) return new FireResult(FireOutcome.ShooterDestroyed, 0, 0, 0, chance);
            if (targetDestroyed) return new FireResult(FireOutcome.TargetDestroyed, 0, 0, 0, chance);
            if (!InRange(shooter.Position, targetPosition))
            {
                return new FireResult(FireOutcome.OutOfRange, 0, 0, 0, chance);
            }
            var shots = shooter.UnitCount;
            var hits = 0;
            for (var i = 0; i < shots; i++)
            {
                if (_random.NextDouble() < chance) hits++;
            }
            var before = health();
            damage(hits * _stats.RiflemenDamage);
            if (targetSection != null) targetSection.LastAttacker = new Attacker(shooter.Id);
            return new FireResult(FireOutcome.Fired, shots, hits, before - health(), chance);
        }

        /// <summary>True when the two points are within Riflemen range. The one range check, used by fire and by targeting.</summary>
        internal bool InRange(MapPoint a, MapPoint b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return dx * dx + dy * dy <= _stats.RiflemenRange * _stats.RiflemenRange;
        }

        /// <summary>The fixed battle-time step in seconds. The core owns it, so the same commands replay the same battle (TEC-008).</summary>
        public const double StepSeconds = 0.1;

        /// <summary>
        /// Advances the battle by one <see cref="StepSeconds"/>. This is the only public way time passes (TEC-008): the Officers
        /// walk along their move direction, then every Section runs its default behaviour.
        /// </summary>
        public void Step() { Tick(StepSeconds); StepCount++; }

        /// <summary>
        /// Advances the battle by <paramref name="seconds"/> of battle time: every living Officer walks along its move direction
        /// (PCH-001), then every living Section, in SectionId order, runs its default behaviour (DAI-001..006).
        /// Deterministic: no clock, only the battle's seeded random source (TEC-008). Internal so only <see cref="Step"/> (and tests) advance time.
        /// </summary>
        internal void Tick(double seconds)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            foreach (var officer in _officers)
            {
                if (officer.IsDestroyed) continue;
                var dir = officer.MoveDirection;
                if (dir.X == 0 && dir.Y == 0) continue;
                var step = _stats.OfficerSpeed * seconds;
                officer.Position = new MapPoint(
                    Math.Min(_map.Width, Math.Max(0, officer.Position.X + dir.X * step)),
                    Math.Min(_map.Height, Math.Max(0, officer.Position.Y + dir.Y * step)));
            }
            foreach (var section in _sections.Values)
            {
                if (!section.IsDestroyed) DefaultBehaviour.Run(this, section, seconds);
            }
        }

        /// <summary>
        /// Applies one Officer command (PCH-009, DLV-013). This is the only way Officer move, aim and fire change the battle.
        /// An accepted command is recorded in <see cref="CommandLog"/>; a rejected one changes nothing and says why.
        /// Move and aim touch only the Officer (PCH-004). Fire is a stand-in hitscan: the shot goes along the Officer's aim and
        /// hits the nearest living enemy Section whose centre it passes within the hit radius, up to the rifle range.
        /// </summary>
        public OfficerCommandResult ApplyOfficerCommand(IBattleCommand command)
        {
            if (!(command is OfficerCommand oc)) return OfficerCommandResult.Rejected("Not an Officer command.");
            if (oc.Officer < 0 || oc.Officer >= _officers.Count) return OfficerCommandResult.Rejected("Officer " + oc.Officer + " does not exist.");
            var officer = _officers[oc.Officer];
            if (officer.IsDestroyed) return OfficerCommandResult.Rejected("Officer " + oc.Officer + " is destroyed and cannot be commanded.");
            switch (oc)
            {
                case OfficerAimCommand a:
                    if (!IsFinite(a.Radians)) return OfficerCommandResult.Rejected("Aim must be a finite number.");
                    officer.AimRadians = a.Radians;
                    Log(command);
                    return OfficerCommandResult.Accepted();
                case OfficerMoveCommand m:
                    if (!IsFinite(m.DirectionX) || !IsFinite(m.DirectionY))
                        return OfficerCommandResult.Rejected("A move needs a finite direction.");
                    // The speed cap holds whatever the client sends: the direction is cut to length 1 here, and the speed is the stats value.
                    var length = Math.Sqrt(m.DirectionX * m.DirectionX + m.DirectionY * m.DirectionY);
                    var scale = length > 1 ? 1 / length : 1;
                    officer.MoveDirection = new MapPoint(m.DirectionX * scale, m.DirectionY * scale);
                    Log(command);
                    return OfficerCommandResult.Accepted();
                case OfficerFireCommand _:
                    var shot = ResolveRifleShot(officer);
                    Log(command);
                    return OfficerCommandResult.Accepted(shot);
                default:
                    return OfficerCommandResult.Rejected("Unknown Officer command.");
            }
        }

        private void Log(IBattleCommand command) { _commandLog.Add(new LoggedCommand(StepCount, command)); }

        /// <summary>
        /// Replays a command log on a fresh battle (TEC-008): for each step number in turn, applies that step's commands in log
        /// order, then calls <see cref="Step"/>, until <paramref name="steps"/> steps have run. Commands logged at step
        /// <paramref name="steps"/> (after the last step) are applied last, without a further step.
        /// It is a divergence detector: a command that was accepted when it was logged but is rejected now throws
        /// <see cref="InvalidOperationException"/> with the step and the reason, and so does a logged step outside 0..<paramref name="steps"/>.
        /// </summary>
        public void Replay(IEnumerable<LoggedCommand> log, int steps)
        {
            var queue = new List<LoggedCommand>(log);
            foreach (var l in queue)
            {
                if (l.Step < 0 || l.Step > steps) throw new InvalidOperationException("Replay: a command is logged at step " + l.Step + ", outside 0.." + steps + ".");
            }
            var next = 0;
            for (var step = 0; step <= steps; step++)
            {
                while (next < queue.Count && queue[next].Step <= step)
                {
                    var result = ApplyOfficerCommand(queue[next].Command);
                    if (!result.IsAccepted) throw new InvalidOperationException("Replay diverged at step " + step + ": " + result.Reason);
                    next++;
                }
                if (step < steps) Step();
            }
        }

        private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        private OfficerShot ResolveRifleShot(Officer officer)
        {
            var dx = Math.Cos(officer.AimRadians);
            var dy = Math.Sin(officer.AimRadians);
            Section? best = null;
            var bestAlong = double.MaxValue;
            foreach (var section in _sections.Values)
            {
                if (section.Side == officer.Side || section.IsDestroyed) continue;
                var rx = section.Position.X - officer.Position.X;
                var ry = section.Position.Y - officer.Position.Y;
                var along = rx * dx + ry * dy;
                if (along < 0 || along > _stats.OfficerRifleRange) continue;
                var across = Math.Abs(rx * dy - ry * dx);
                if (across > _stats.OfficerRifleHitRadius) continue;
                if (along < bestAlong) { best = section; bestAlong = along; }
            }
            if (best == null) return new OfficerShot(null, 0, _stats.OfficerRifleRange);
            var before = best.Health;
            best.TakeDamage(_stats.OfficerRifleDamage);
            best.LastAttacker = new Attacker(officer); // DAI-006: the Section will prefer to shoot back at the Officer.
            return new OfficerShot(best.Id, before - best.Health, bestAlong);
        }
    }
}
