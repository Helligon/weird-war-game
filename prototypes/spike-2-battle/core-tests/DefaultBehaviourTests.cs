using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

public class DefaultBehaviourTests
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

    private static string StatsText() => File.ReadAllText(Path.Combine(DataDir, "stats.json"));
    private static UnitStats StatsWith(params (string Key, double Value)[] values)
    {
        var text = StatsText();
        foreach (var (key, value) in values)
        {
            var pattern = "\"" + key + "\"\\s*:\\s*[0-9.]+";
            Assert.Matches(pattern, text);
            text = System.Text.RegularExpressions.Regex.Replace(text, pattern,
                "\"" + key + "\": " + value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return UnitStats.Load(text);
    }
    private static UnitStats StatsWithRange(double range) => StatsWith(("range", range));
    private static BattleParameters Params() => BattleParameters.Load(File.ReadAllText(Path.Combine(DataDir, "parameters.json")));

    private static BattleMap OpenMap() => new BattleMap(100, 60, new List<CoverObject>());
    private static BattleMap MapWithCover() =>
        new BattleMap(100, 60, new List<CoverObject> { new CoverObject("ruin", new MapPoint(20, 22), 4) });

    private static BattleState Battle(BattleMap map, double range = 20, ulong seed = 7) =>
        BattleState.SetUp(map, StatsWithRange(range), Params(), seed);

    private static BattleState Battle(BattleMap map, UnitStats stats, BattleParameters? p = null, ulong seed = 7) =>
        BattleState.SetUp(map, stats, p ?? Params(), seed);

    private static Section Player(BattleState b) => b.Find(BattleState.PlayerSectionId)!;
    private static Section Enemy(BattleState b) => b.Find(BattleState.EnemySectionId)!;

    private static Section AddEnemy(BattleState b, int id, double x, double y)
    {
        var s = new Section(new SectionId(id), Side.Enemy, UnitType.Riflemen, b.Stats.RiflemenMaxUnits, b.Stats.RiflemenHealthPerUnit, new MapPoint(x, y));
        b.Add(s);
        return s;
    }

    private static double Distance(Section a, Section b)
    {
        var dx = a.Position.X - b.Position.X;
        var dy = a.Position.Y - b.Position.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    [Fact]
    public void DAI_001_section_with_no_orders_moves_to_engage_the_enemy()
    {
        var b = Battle(OpenMap());
        var startX = Player(b).Position.X;
        var startDistance = Distance(Player(b), Enemy(b));

        for (var i = 0; i < 60; i++) b.Tick(1);

        Assert.True(Player(b).Position.X > startX);
        Assert.True(Distance(Player(b), Enemy(b)) < startDistance);
        // It stops once the enemy is in range, and fires.
        Assert.True(Distance(Player(b), Enemy(b)) <= b.Stats.RiflemenRange);
        Assert.True(Enemy(b).Health < Enemy(b).MaxUnits * Enemy(b).HealthPerUnit);
    }

    [Fact]
    public void DAI_001_section_with_no_orders_takes_nearby_cover_that_is_off_its_straight_path()
    {
        var b = Battle(MapWithCover());
        Enemy(b).IsHolding = true;
        Enemy(b).MoveTo(new MapPoint(36, 30));

        for (var i = 0; i < 20; i++) b.Tick(1);

        // Walking straight at the enemy would stop at (16, 30), 8 m from the cover at (20, 22). It must turn aside.
        var spot = Player(b).Position;
        Assert.Same(b.Map.CoverObjects[0], b.Map.CoverAt(spot));
        Assert.True(spot.Y < 30);
    }

    [Fact]
    public void DAI_001_section_stays_in_cover_while_its_target_is_in_range()
    {
        var b = Battle(MapWithCover());
        Enemy(b).IsHolding = true;
        Enemy(b).MoveTo(new MapPoint(36, 30));
        for (var i = 0; i < 20; i++) b.Tick(1);
        var spot = Player(b).Position;

        for (var i = 0; i < 20; i++) b.Tick(1);

        Assert.Equal(spot.X, Player(b).Position.X);
        Assert.Equal(spot.Y, Player(b).Position.Y);
        Assert.True(Enemy(b).Health < Enemy(b).MaxUnits * Enemy(b).HealthPerUnit);
    }

    [Fact]
    public void DAI_001_section_already_in_cover_does_not_move_deeper_in_while_a_target_is_in_range()
    {
        var b = Battle(new BattleMap(100, 60, new List<CoverObject> { new CoverObject("wall", new MapPoint(25, 30), 6) }));
        Enemy(b).IsHolding = true;
        Enemy(b).MoveTo(new MapPoint(40, 30));
        Player(b).MoveTo(new MapPoint(20, 30));

        for (var i = 0; i < 10; i++) b.Tick(1);

        Assert.Equal(20, Player(b).Position.X);
        Assert.Equal(30, Player(b).Position.Y);
    }

    [Fact]
    public void DAI_001_section_never_backs_away_to_reach_cover()
    {
        var b = Battle(new BattleMap(100, 60, new List<CoverObject> { new CoverObject("behind", new MapPoint(4, 30), 2) }));
        Enemy(b).IsHolding = true;
        Enemy(b).MoveTo(new MapPoint(36, 30));
        Player(b).MoveTo(new MapPoint(20, 30));

        for (var i = 0; i < 10; i++) b.Tick(1);

        Assert.Equal(20, Player(b).Position.X);
    }

    [Fact]
    public void CMD_019_default_behaviour_fires_with_the_moving_penalty_while_moving_to_cover()
    {
        // The Section starts in range and out of cover, so its first volley is fired while it steps toward cover.
        BattleState Run(string penalty)
        {
            var p = BattleParameters.Load("{\"P-CMD-01\": 2, \"P-CMD-03\": " + penalty + "}");
            var b = Battle(MapWithCover(), StatsWithRange(20), p);
            Enemy(b).IsHolding = true;
            Enemy(b).MoveTo(new MapPoint(36, 30));
            Player(b).MoveTo(new MapPoint(16, 30));
            b.Step();
            return b;
        }
        var full = Enemy(Run("0"));
        var none = Enemy(Run("1"));

        Assert.True(full.Health < full.MaxUnits * full.HealthPerUnit);
        Assert.Equal(none.MaxUnits * none.HealthPerUnit, none.Health);
    }

    [Fact]
    public void DEC_098_section_fires_one_volley_per_volley_interval()
    {
        var stats = StatsWith(("range", 20), ("accuracy", 1), ("P-HIE-02", 100));
        var b = Battle(OpenMap(), stats);
        Player(b).IsHolding = true;
        Enemy(b).IsHolding = true;
        Enemy(b).MoveTo(new MapPoint(20, 30));
        var start = Enemy(b).Health;

        for (var i = 0; i < 10; i++) b.Tick(1); // interval is 2 s: volleys at 1, 3, 5, 7 and 9 s

        Assert.Equal(5 * b.Stats.RiflemenMaxUnits * b.Stats.RiflemenDamage, start - Enemy(b).Health);
    }

    [Fact]
    public void DAI_006_section_targets_the_nearest_enemy_in_range()
    {
        var b = Battle(OpenMap(), range: 90);
        Enemy(b).MoveTo(new MapPoint(60, 30));
        var near = AddEnemy(b, 3, 30, 30);
        Player(b).MoveTo(new MapPoint(10, 30));

        var choice = DefaultBehaviour.ChooseTarget(b, Player(b));

        Assert.NotNull(choice);
        Assert.Same(near, choice!.Value.Section);
    }

    [Fact]
    public void DAI_006_section_prefers_the_enemy_attacking_it_over_a_nearer_one()
    {
        var b = Battle(OpenMap(), range: 90);
        var near = AddEnemy(b, 3, 30, 30);
        var far = Enemy(b);
        far.MoveTo(new MapPoint(60, 30));
        Player(b).MoveTo(new MapPoint(10, 30));
        Assert.Same(near, DefaultBehaviour.ChooseTarget(b, Player(b))!.Value.Section);

        var result = b.Fire(far.Id, Player(b).Id);

        Assert.Equal(FireOutcome.Fired, result.Outcome);
        Assert.Same(far, DefaultBehaviour.ChooseTarget(b, Player(b))!.Value.Section);
    }

    [Fact]
    public void DAI_006_section_can_target_the_enemy_officer_when_it_is_nearest()
    {
        var b = Battle(OpenMap(), range: 90);
        Enemy(b).MoveTo(new MapPoint(60, 30));
        var officer = new Officer(Side.Enemy, b.Stats.OfficerHealth, new MapPoint(25, 30));
        b.AddOfficer(officer);
        Player(b).MoveTo(new MapPoint(10, 30));

        var choice = DefaultBehaviour.ChooseTarget(b, Player(b));

        Assert.NotNull(choice);
        Assert.Same(officer, choice!.Value.Officer);
        Assert.Null(choice.Value.Section);
        for (var i = 0; i < 4; i++) b.Tick(1);
        Assert.True(officer.Health < b.Stats.OfficerHealth);
    }

    [Fact]
    public void DAI_006_no_target_is_chosen_when_no_enemy_is_in_range()
    {
        var b = Battle(OpenMap(), range: 20);

        Assert.Null(DefaultBehaviour.ChooseTarget(b, Player(b)));
    }

    [Fact]
    public void DAI_004_section_holding_after_stop_keeps_its_position_and_fires()
    {
        var b = Battle(OpenMap());
        Player(b).IsHolding = true;
        Enemy(b).MoveTo(new MapPoint(60, 30));
        var start = Player(b).Position;
        var fullHealth = Enemy(b).MaxUnits * Enemy(b).HealthPerUnit;

        for (var i = 0; i < 60; i++) b.Tick(1);

        Assert.Equal(start.X, Player(b).Position.X);
        Assert.Equal(start.Y, Player(b).Position.Y);
        Assert.True(Distance(Player(b), Enemy(b)) <= b.Stats.RiflemenRange);
        Assert.True(Enemy(b).Health < fullHealth);
    }

    [Fact]
    public void TEC_008_same_seed_gives_the_same_battle()
    {
        var a = Battle(MapWithCover(), range: 20, seed: 3);
        var c = Battle(MapWithCover(), range: 20, seed: 3);

        for (var i = 0; i < 80; i++) { a.Tick(0.5); c.Tick(0.5); }

        Assert.Equal(Player(a).Position.X, Player(c).Position.X);
        Assert.Equal(Player(a).Position.Y, Player(c).Position.Y);
        Assert.Equal(Player(a).Health, Player(c).Health);
        Assert.Equal(Enemy(a).Position.X, Enemy(c).Position.X);
        Assert.Equal(Enemy(a).Position.Y, Enemy(c).Position.Y);
        Assert.Equal(Enemy(a).Health, Enemy(c).Health);
    }

    [Fact]
    public void DLV_014_a_stats_file_without_the_cover_search_range_is_an_error()
    {
        var text = System.Text.RegularExpressions.Regex.Replace(StatsText(), ",?\\s*\"P-DAI-01\"\\s*:\\s*[0-9.]+", "");

        Assert.DoesNotContain("P-DAI-01", text);
        Assert.Throws<DataLoadException>(() => UnitStats.Load(text));
    }

    [Fact]
    public void DLV_014_a_stats_file_without_the_volley_interval_is_an_error()
    {
        var text = System.Text.RegularExpressions.Regex.Replace(StatsText(), "\"volleyInterval\"\\s*:\\s*[0-9.]+,?", "");

        Assert.Throws<DataLoadException>(() => UnitStats.Load(text));
    }
}
