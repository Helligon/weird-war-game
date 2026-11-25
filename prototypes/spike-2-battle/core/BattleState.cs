using System;
using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>What one volley did.</summary>
    public readonly struct FireResult
    {
        public bool InRange { get; }
        public int Shots { get; }
        public int Hits { get; }
        public int DamageDealt { get; }
        /// <summary>The hit chance used for each shot, after any moving penalty (CMD-019).</summary>
        public double HitChance { get; }

        public FireResult(bool inRange, int shots, int hits, int damageDealt, double hitChance)
        {
            InRange = inRange; Shots = shots; Hits = hits; DamageDealt = damageDealt; HitChance = hitChance;
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

        private readonly Dictionary<int, Section> _sections = new Dictionary<int, Section>();
        private readonly List<Officer> _officers = new List<Officer>();
        private readonly UnitStats _stats;
        private readonly BattleParameters _parameters;

        public IEnumerable<Section> Sections => _sections.Values;
        public IReadOnlyList<Officer> Officers => _officers;

        private BattleState(UnitStats stats, BattleParameters parameters)
        {
            _stats = stats;
            _parameters = parameters;
        }

        /// <summary>
        /// ROS-001: both sides field a Riflemen Section, sized from the stats file (P-HIE-01, P-HIE-02). Spike stand-in:
        /// the player has 1 Section and 1 Officer, the enemy 1 Section and no Officer. Each stands just inside its own
        /// edge strip (BMP-004), at the middle of the map's height.
        /// </summary>
        public static BattleState SetUp(BattleMap map, UnitStats stats, BattleParameters parameters)
        {
            var state = new BattleState(stats, parameters);
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

        /// <summary>The hit chance of one shot: full accuracy standing still, reduced by P-CMD-03 while moving (CMD-019).</summary>
        public double HitChance(Section shooter) => HitChance(shooter.IsMoving);

        /// <summary>The hit chance of one Riflemen shot, moving or standing still (CMD-019).</summary>
        public double HitChance(bool moving) =>
            _stats.RiflemenAccuracy * (moving ? 1.0 - _parameters.MovingAccuracyPenalty : 1.0);

        /// <summary>
        /// Placeholder ranged fire: each of the shooter's Units fires one shot; each hit takes damage off the target's
        /// health. Out of range, or a destroyed shooter or target, does nothing. Random rolls come only from the given source.
        /// </summary>
        public FireResult Fire(SectionId shooterId, SectionId targetId, SeededRandom random)
        {
            var shooter = Find(shooterId) ?? throw new ArgumentException(shooterId + " does not exist.");
            return Fire(shooter, targetId, random);
        }

        /// <summary>As <see cref="Fire(SectionId, SectionId, SeededRandom)"/>, for a shooter given directly (e.g. a moving copy for a demo).</summary>
        public FireResult Fire(Section shooter, SectionId targetId, SeededRandom random)
        {
            var target = Find(targetId) ?? throw new ArgumentException(targetId + " does not exist.");
            var chance = HitChance(shooter);
            var dx = shooter.Position.X - target.Position.X;
            var dy = shooter.Position.Y - target.Position.Y;
            if (shooter.IsDestroyed || target.IsDestroyed || dx * dx + dy * dy > _stats.RiflemenRange * _stats.RiflemenRange)
            {
                return new FireResult(false, 0, 0, 0, chance);
            }
            var shots = shooter.UnitCount;
            var hits = 0;
            for (var i = 0; i < shots; i++)
            {
                if (random.NextDouble() < chance) hits++;
            }
            var before = target.Health;
            target.TakeDamage(hits * _stats.RiflemenDamage);
            return new FireResult(true, shots, hits, before - target.Health, chance);
        }
    }
}
