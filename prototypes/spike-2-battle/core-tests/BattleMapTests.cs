using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

public class BattleMapTests
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

    private static BattleMap LoadMap() => BattleMapLoader.Load(File.ReadAllText(Path.Combine(DataDir, "map.json")));
    private static UnitStats LoadStats() => UnitStats.Load(File.ReadAllText(Path.Combine(DataDir, "stats.json")));

    [Fact]
    public void BMP_004_each_side_has_its_own_map_edge_and_the_two_edges_differ()
    {
        var map = LoadMap();

        var player = map.EdgeOf(Side.Player);
        var enemy = map.EdgeOf(Side.Enemy);

        Assert.NotNull(player);
        Assert.NotNull(enemy);
        Assert.NotSame(player, enemy);
        Assert.NotEqual(player.X, enemy.X);
        Assert.Equal(MapEdgeSide.West, player.Side);
        Assert.Equal(MapEdgeSide.East, enemy.Side);
        Assert.Throws<ArgumentOutOfRangeException>(() => map.EdgeOf((Side)0));
    }

    [Fact]
    public void BMP_005_map_has_cover_with_positions_the_section_logic_can_read()
    {
        var map = LoadMap();

        Assert.NotEmpty(map.CoverObjects);
        var first = map.CoverObjects[0];
        Assert.Same(first, map.CoverAt(first.Position));
        Assert.Null(map.CoverAt(new MapPoint(1, 1)));
        Assert.All(map.CoverObjects, c =>
        {
            Assert.InRange(c.Position.X, 0, map.Width);
            Assert.InRange(c.Position.Y, 0, map.Height);
        });
    }

    [Fact]
    public void BMP_010_crossing_time_at_placeholder_riflemen_speed_is_60_to_90_seconds()
    {
        var map = LoadMap();
        var stats = LoadStats();

        var seconds = map.CrossingSeconds(stats.RiflemenSpeed);

        Assert.InRange(seconds, 60.0, 90.0); // P-BMP-01
    }

    // DLV-014: a stats file with a missing value is an error, so these tests give every value.
    private static string StatsWithSpeed(string speed) =>
        "{ \"riflemen\": { \"speed\": " + speed + ", \"P-HIE-01\": 8, \"P-HIE-02\": 3, \"range\": 40, \"damage\": 1, \"accuracy\": 0.6, \"volleyInterval\": 2, \"coverSearchRange\": 25 }, \"officer\": { \"health\": 10 } }";

    [Fact]
    public void BMP_010_crossing_time_uses_the_speed_from_the_data_file_not_a_hard_coded_value()
    {
        var map = LoadMap();
        var slow = UnitStats.Load(StatsWithSpeed("1.0"));
        var fast = UnitStats.Load(StatsWithSpeed("2.0"));

        Assert.Equal(map.EdgeDistance / 1.0, map.CrossingSeconds(slow.RiflemenSpeed), 6);
        Assert.Equal(map.EdgeDistance / 2.0, map.CrossingSeconds(fast.RiflemenSpeed), 6);
        Assert.NotEqual(map.CrossingSeconds(slow.RiflemenSpeed), map.CrossingSeconds(fast.RiflemenSpeed));
    }

    [Fact]
    public void BMP_010_map_size_comes_from_the_map_file()
    {
        var small = BattleMapLoader.Load("{ \"width\": 50, \"height\": 20 }");

        Assert.Equal(50, small.EdgeDistance);
        Assert.Empty(small.CoverObjects);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{ \"width\": 100, \"height\": 60, \"cover\": [ { \"id\": \"a\", \"x\": 6, \"y\": 30, \"radius\": 4 } ] }")] // disc reaches into the west edge strip
    [InlineData("{ \"width\": 100, \"height\": 60, \"cover\": [ { \"id\": \"a\", \"x\": 50, \"y\": 58, \"radius\": 4 } ] }")] // disc reaches past the north side
    [InlineData("{ \"height\": 10 }")]
    [InlineData("{ \"width\": 10, \"height\": 10, \"cover\": [ { \"id\": \"a\", \"x\": 11, \"y\": 1, \"radius\": 1 } ] }")]
    [InlineData("{ \"width\": 10, \"height\": 10, \"cover\": [ { \"id\": \"a\", \"x\": 1, \"y\": 1, \"radius\": 0 } ] }")]
    [InlineData("{ \"width\": 20, \"height\": 10, \"cover\": [ { \"id\": \"a\", \"x\": 8, \"y\": 5, \"radius\": 1 }, { \"id\": \"a\", \"x\": 12, \"y\": 5, \"radius\": 1 } ] }")]
    public void DEC_087_malformed_map_file_is_rejected_with_a_message(string json)
    {
        Assert.Throws<DataLoadException>(() => BattleMapLoader.Load(json));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{ \"riflemen\": { \"speed\": 0 } }")]
    [InlineData("{ \"riflemen\": { \"speed\": \"fast\" } }")]
    public void DEC_087_malformed_stats_file_is_rejected_with_a_message(string json)
    {
        Assert.Throws<DataLoadException>(() => UnitStats.Load(json));
    }
}
