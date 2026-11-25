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
    private static UnitStats StatsWithRange(double range) =>
        UnitStats.Load(System.Text.RegularExpressions.Regex.Replace(StatsText(), "\"range\"\\s*:\\s*[0-9.]+", "\"range\": " + range));
    private static BattleParameters Params() => BattleParameters.Load(File.ReadAllText(Path.Combine(DataDir, "parameters.json")));

    private static BattleMap OpenMap() => new BattleMap(100, 60, new List<CoverObject>());
    private static BattleMap MapWithCover() =>
        new BattleMap(100, 60, new List<CoverObject> { new CoverObject("ruin", new MapPoint(20, 30), 4) });

    private static BattleState Battle(BattleMap map, double range = 20, ulong seed = 7) =>
        BattleState.SetUp(map, StatsWithRange(range), Params(), seed);

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
    public void DAI_001_section_with_no_orders_takes_nearby_cover_and_stays_in_it_while_firing()
    {
        var b = Battle(MapWithCover());
        Enemy(b).IsHolding = true;
        Enemy(b).MoveTo(new MapPoint(36, 30));

        for (var i = 0; i < 20; i++) b.Tick(1);

        var spot = Player(b).Position;
        Assert.NotNull(b.Map.CoverAt(spot));
        for (var i = 0; i < 10; i++) b.Tick(1);
        Assert.Equal(spot.X, Player(b).Position.X);
        Assert.Equal(spot.Y, Player(b).Position.Y);
        Assert.True(Enemy(b).Health < Enemy(b).MaxUnits * Enemy(b).HealthPerUnit);
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
        Assert.Equal(Player(a).Health, Player(c).Health);
        Assert.Equal(Enemy(a).Position.X, Enemy(c).Position.X);
        Assert.Equal(Enemy(a).Health, Enemy(c).Health);
    }

    [Fact]
    public void DLV_014_a_stats_file_without_the_volley_interval_is_an_error()
    {
        var text = System.Text.RegularExpressions.Regex.Replace(StatsText(), "\"volleyInterval\"\\s*:\\s*[0-9.]+,?", "");

        Assert.Throws<DataLoadException>(() => UnitStats.Load(text));
    }
}
