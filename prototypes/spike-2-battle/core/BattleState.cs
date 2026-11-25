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
        private readonly UnitStats _stats;
        private readonly BattleParameters _parameters;
        private readonly SeededRandom _random;

        /// <summary>All Sections, in SectionId order (stable, TEC-008).</summary>
        public IEnumerable<Section> Sections => _sections.Values;
        public IReadOnlyList<Officer> Officers => _officers;

        private readonly BattleMap _map;
        private readonly List<IBattleCommand> _commandLog = new List<IBattleCommand>();

        /// <summary>PCH-009: every accepted Officer command, in the order it was applied.</summary>
        public IReadOnlyList<IBattleCommand> CommandLog => _commandLog;

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
            var chance = HitChance(moving);
            if (shooter.IsDestroyed) return new FireResult(FireOutcome.ShooterDestroyed, 0, 0, 0, chance);
            if (target.IsDestroyed) return new FireResult(FireOutcome.TargetDestroyed, 0, 0, 0, chance);
            var dx = shooter.Position.X - target.Position.X;
            var dy = shooter.Position.Y - target.Position.Y;
            if (dx * dx + dy * dy > _stats.RiflemenRange * _stats.RiflemenRange)
            {
                return new FireResult(FireOutcome.OutOfRange, 0, 0, 0, chance);
            }
            var shots = shooter.UnitCount;
            var hits = 0;
            for (var i = 0; i < shots; i++)
            {
                if (_random.NextDouble() < chance) hits++;
            }
            var before = target.Health;
            target.TakeDamage(hits * _stats.RiflemenDamage);
            return new FireResult(FireOutcome.Fired, shots, hits, before - target.Health, chance);
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
            switch (oc)
            {
                case OfficerAimCommand a:
                    if (!IsFinite(a.Radians)) return OfficerCommandResult.Rejected("Aim must be a finite number.");
                    officer.AimRadians = a.Radians;
                    _commandLog.Add(command);
                    return OfficerCommandResult.Accepted();
                case OfficerMoveCommand m:
                    if (!IsFinite(m.DirectionX) || !IsFinite(m.DirectionY) || !IsFinite(m.Seconds) || m.Seconds <= 0)
                        return OfficerCommandResult.Rejected("A move needs a finite direction and a duration above zero.");
                    var length = Math.Sqrt(m.DirectionX * m.DirectionX + m.DirectionY * m.DirectionY);
                    var scale = length > 1 ? 1 / length : 1;
                    var step = _stats.OfficerSpeed * m.Seconds * scale;
                    var x = Math.Min(_map.Width, Math.Max(0, officer.Position.X + m.DirectionX * step));
                    var y = Math.Min(_map.Height, Math.Max(0, officer.Position.Y + m.DirectionY * step));
                    officer.Position = new MapPoint(x, y);
                    _commandLog.Add(command);
                    return OfficerCommandResult.Accepted();
                case OfficerFireCommand _:
                    var shot = ResolveRifleShot(officer);
                    _commandLog.Add(command);
                    return OfficerCommandResult.Accepted(shot);
                default:
                    return OfficerCommandResult.Rejected("Unknown Officer command.");
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
            return new OfficerShot(best.Id, before - best.Health, bestAlong);
        }
    }
}
