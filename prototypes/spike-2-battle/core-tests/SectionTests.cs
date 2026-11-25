using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

public class SectionTests
{
    private static string DataDir
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "data", "map.json"))) dir = dir.Parent;
            return Path.Combine(dir!.FullName, "data");
        }
    }

    private static BattleMap Map() => BattleMapLoader.Load(File.ReadAllText(Path.Combine(DataDir, "map.json")));
    private static UnitStats Stats() => UnitStats.Load(File.ReadAllText(Path.Combine(DataDir, "stats.json")));
    private static BattleParameters Params() => BattleParameters.Load(File.ReadAllText(Path.Combine(DataDir, "parameters.json")));

    private static UnitStats StatsWith(int maxUnits = 8, int healthPerUnit = 3, double range = 40, int damage = 1, double accuracy = 0.6) =>
        new UnitStats(1.5, maxUnits, healthPerUnit, range, damage, accuracy, 10);

    private static BattleState Battle(UnitStats? stats = null, BattleParameters? p = null) =>
        BattleState.SetUp(Map(), stats ?? Stats(), p ?? Params());

    private static Section Player(BattleState b) => b.Find(BattleState.PlayerSectionId)!;
    private static Section Enemy(BattleState b) => b.Find(BattleState.EnemySectionId)!;

    [Fact]
    public void ROS_001_both_sides_field_a_riflemen_section()
    {
        var b = Battle();

        Assert.Equal(UnitType.Riflemen, Player(b).Type);
        Assert.Equal(Side.Player, Player(b).Side);
        Assert.Equal(UnitType.Riflemen, Enemy(b).Type);
        Assert.Equal(Side.Enemy, Enemy(b).Side);
        Assert.Equal(2, b.Sections.Count());
    }

    [Fact]
    public void HIE_001_HIE_002_a_riflemen_section_is_one_or_more_units_of_the_riflemen_type()
    {
        var s = Player(Battle());

        Assert.True(s.UnitCount >= 1);
        Assert.Equal(Stats().RiflemenMaxUnits, s.UnitCount);
        Assert.Equal(UnitType.Riflemen, s.Type);
    }

    [Fact]
    public void HIE_002_a_section_with_no_units_is_not_valid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Section(new SectionId(9), Side.Player, UnitType.Riflemen, 0, 3, new MapPoint(0, 0)));
    }

    [Fact]
    public void HIE_003_a_section_with_a_single_unit_is_valid()
    {
        var b = Battle(StatsWith(maxUnits: 1));

        Assert.Equal(1, Player(b).UnitCount);
        Assert.Equal(1, Enemy(b).UnitCount);
    }

    [Fact]
    public void HIE_006_the_officer_is_a_separate_unit_not_counted_in_any_section()
    {
        var b = Battle();

        var officer = Assert.Single(b.Officers);
        Assert.Equal(Side.Player, officer.Side);
        Assert.Equal(Stats().OfficerHealth, officer.Health);
        Assert.Equal(Stats().RiflemenMaxUnits, Player(b).UnitCount);
        Assert.Equal(Stats().RiflemenMaxUnits, Enemy(b).UnitCount);
    }

    [Fact]
    public void HIE_004_reducing_health_drops_the_unit_count_to_match()
    {
        var b = Battle(StatsWith(maxUnits: 4, healthPerUnit: 3));
        var s = Player(b);
        Assert.Equal(4, s.UnitCount);

        s.TakeDamage(3);
        Assert.Equal(3, s.UnitCount);
        s.TakeDamage(6);
        Assert.Equal(1, s.UnitCount);
    }

    [Theory]
    [InlineData(12, 3, 4)]
    [InlineData(11, 3, 4)]
    [InlineData(10, 3, 4)]
    [InlineData(9, 3, 3)]
    [InlineData(4, 3, 2)]
    [InlineData(1, 3, 1)]
    public void HIE_010_unit_count_is_health_over_health_per_unit_rounded_up(int health, int perUnit, int expected)
    {
        var s = new Section(new SectionId(9), Side.Player, UnitType.Riflemen, 4, perUnit, new MapPoint(0, 0));
        s.TakeDamage(12 - health);

        Assert.Equal(health, s.Health);
        Assert.Equal(expected, s.UnitCount);
    }

    [Fact]
    public void HIE_004_a_section_at_health_zero_is_destroyed_and_has_no_units()
    {
        var s = Player(Battle());

        s.TakeDamage(s.Health + 50);

        Assert.Equal(0, s.Health);
        Assert.True(s.IsDestroyed);
        Assert.Equal(0, s.UnitCount);
    }

    [Fact]
    public void HIE_004_data_changed_stats_change_section_sizes_in_a_new_battle()
    {
        var a = Battle(StatsWith(maxUnits: 5, healthPerUnit: 2));
        var b = Battle(StatsWith(maxUnits: 12, healthPerUnit: 7));

        Assert.Equal(5, Player(a).UnitCount);
        Assert.Equal(10, Player(a).Health);
        Assert.Equal(12, Player(b).UnitCount);
        Assert.Equal(84, Player(b).Health);
    }

    [Fact]
    public void DLV_014_stats_file_with_a_missing_value_is_an_error()
    {
        var text = File.ReadAllText(Path.Combine(DataDir, "stats.json"));
        foreach (var key in new[] { "maxUnits", "healthPerUnit", "range", "damage", "accuracy", "health" })
        {
            var broken = text.Replace("\"" + key + "\"", "\"x-" + key + "\"");
            Assert.Throws<DataLoadException>(() => UnitStats.Load(broken));
        }
        Assert.Throws<CommandFormatException>(() => BattleParameters.Load("{\"P-CMD-01\": 2}"));
    }

    [Fact]
    public void DLV_012_the_battle_state_is_a_battle_view_for_command_validation()
    {
        var b = Battle();

        var result = CommandValidator.Validate(new TargetCommand(BattleState.PlayerSectionId, BattleState.EnemySectionId), b, Params());
        var bad = CommandValidator.Validate(new TargetCommand(BattleState.PlayerSectionId, new SectionId(77)), b, Params());

        Assert.True(result.IsAccepted);
        Assert.False(bad.IsAccepted);
    }

    // Two sections 10 m apart, so range is easy to control. Large shooter so statistics are stable.
    private static (BattleState, Section, Section) Duel(UnitStats stats, bool moving, double distance = 10)
    {
        var b = BattleState.SetUp(Map(), stats, Params());
        var shooter = new Section(new SectionId(10), Side.Player, UnitType.Riflemen, 1000, 3, new MapPoint(10, 10), moving);
        var target = new Section(new SectionId(11), Side.Enemy, UnitType.Riflemen, 1000, 3, new MapPoint(10 + distance, 10));
        b.Add(shooter);
        b.Add(target);
        return (b, shooter, target);
    }

    [Fact]
    public void Ranged_fire_an_enemy_in_range_loses_health()
    {
        var (b, shooter, target) = Duel(StatsWith(), moving: false);
        var before = target.Health;

        var r = b.Fire(shooter.Id, target.Id, new SeededRandom(1));

        Assert.True(r.InRange);
        Assert.True(r.Hits > 0);
        Assert.True(target.Health < before);
        Assert.Equal(before - r.DamageDealt, target.Health);
    }

    [Fact]
    public void Ranged_fire_an_enemy_out_of_range_is_not_hurt()
    {
        var (b, shooter, target) = Duel(StatsWith(range: 40), moving: false, distance: 41);
        var before = target.Health;

        var r = b.Fire(shooter.Id, target.Id, new SeededRandom(1));

        Assert.False(r.InRange);
        Assert.Equal(before, target.Health);
    }

    [Fact]
    public void CMD_019_a_moving_section_fires_at_the_reduced_accuracy_and_a_standing_one_at_full()
    {
        var stats = StatsWith(accuracy: 0.8);
        var (b, moving, target) = Duel(stats, moving: true);
        var still = new Section(new SectionId(12), Side.Player, UnitType.Riflemen, 1000, 3, new MapPoint(10, 12), false);
        b.Add(still);

        Assert.Equal(0.8 * (1 - Params().MovingAccuracyPenalty), b.HitChance(moving), 9);
        Assert.Equal(0.8, b.HitChance(still), 9);

        var rm = b.Fire(moving.Id, target.Id, new SeededRandom(5));
        var rs = b.Fire(still.Id, target.Id, new SeededRandom(5));
        Assert.True(rm.Hits < rs.Hits);
        Assert.Equal(rm.HitChance, rm.Hits / (double)rm.Shots, 1);
        Assert.Equal(rs.HitChance, rs.Hits / (double)rs.Shots, 1);
    }

    [Fact]
    public void CMD_019_the_penalty_follows_a_changed_P_CMD_03()
    {
        var lenient = new BattleParameters(2, 0.1);
        var harsh = new BattleParameters(2, 0.9);
        var stats = StatsWith(accuracy: 1.0);
        var shooter = new Section(new SectionId(10), Side.Player, UnitType.Riflemen, 4, 3, new MapPoint(0, 0), true);

        Assert.Equal(0.9, BattleState.SetUp(Map(), stats, lenient).HitChance(shooter), 9);
        Assert.Equal(0.1, BattleState.SetUp(Map(), stats, harsh).HitChance(shooter), 9);
        Assert.Equal(0.5, BattleParameters.Load("{\"P-CMD-01\": 2, \"P-CMD-03\": 0.5}").MovingAccuracyPenalty, 9);
    }

    [Fact]
    public void TEC_008_the_same_seed_and_inputs_give_the_same_fire_result()
    {
        FireResult Run(ulong seed)
        {
            var (b, shooter, target) = Duel(StatsWith(), moving: true);
            var r = b.Fire(shooter.Id, target.Id, new SeededRandom(seed));
            Assert.Equal(r.DamageDealt, 3000 - target.Health);
            return r;
        }

        var a = Run(42);
        var again = Run(42);
        var other = Run(43);

        Assert.Equal(a.Hits, again.Hits);
        Assert.Equal(a.DamageDealt, again.DamageDealt);
        Assert.NotEqual(a.Hits, other.Hits);
    }

    [Fact]
    public void TEC_008_the_seeded_random_sequence_is_fixed()
    {
        var r = new SeededRandom(0);
        var first = new[] { r.NextDouble(), r.NextDouble() };
        var r2 = new SeededRandom(0);

        Assert.Equal(first[0], r2.NextDouble());
        Assert.Equal(first[1], r2.NextDouble());
        Assert.All(first, v => Assert.InRange(v, 0.0, 0.999999999));
    }
}
