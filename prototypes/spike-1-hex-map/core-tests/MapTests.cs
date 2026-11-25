using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Core.Tests;

public class MapTests
{
    private static string SampleJson() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "maps", "sample.json"));

    private static HexMap Sample() => MapLoader.Load(SampleJson());

    private static HexCoord At(int col, int row) => HexCoord.FromOffset(new OffsetCoord(col, row));

    [Fact]
    public void MAP_019_sample_map_is_20_columns_by_12_rows_landscape_and_every_cell_has_terrain()
    {
        var map = Sample();

        Assert.Equal(20, map.Width);
        Assert.Equal(12, map.Height);
        Assert.True(map.Width > map.Height, "landscape");
        for (var c = 0; c < map.Width; c++)
        {
            for (var r = 0; r < map.Height; r++)
            {
                Assert.True(Enum.IsDefined(map.TerrainAt(At(c, r))));
            }
        }
    }

    [Fact]
    public void MAP_019_size_is_read_from_the_file()
    {
        var map = MapLoader.Load("{\"width\": 4, \"height\": 7, \"default\": \"plain\"}");

        Assert.Equal(4, map.Width);
        Assert.Equal(7, map.Height);
    }

    [Fact]
    public void MAP_018_sample_map_has_mountain_sea_and_river()
    {
        var map = Sample();
        var found = new HashSet<TerrainType>();
        for (var c = 0; c < map.Width; c++)
            for (var r = 0; r < map.Height; r++)
                found.Add(map.TerrainAt(At(c, r)));

        Assert.Contains(TerrainType.Plain, found);
        Assert.Contains(TerrainType.Mountain, found);
        Assert.Contains(TerrainType.Sea, found);
        Assert.Contains(TerrainType.River, found);
    }

    [Fact]
    public void MAP_018_loading_twice_gives_identical_maps()
    {
        var a = MapLoader.Load(SampleJson());
        var b = MapLoader.Load(SampleJson());

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void MAP_018_a_different_map_is_not_equal()
    {
        var a = MapLoader.Load("{\"width\": 3, \"height\": 3, \"default\": \"plain\"}");
        var b = MapLoader.Load("{\"width\": 3, \"height\": 3, \"default\": \"plain\", \"terrain\": {\"sea\": [[1,1]]}}");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MAP_017_interior_cell_has_six_neighbours_on_the_map()
    {
        var map = Sample();

        Assert.Equal(6, map.Neighbours(At(10, 6)).Count);
    }

    [Fact]
    public void MAP_017_edge_and_corner_cells_have_fewer_than_six_neighbours_all_inside_the_map()
    {
        var map = Sample();

        foreach (var cell in new[] { At(0, 0), At(19, 11), At(10, 0), At(0, 6), At(19, 6), At(15, 11) })
        {
            var neighbours = map.Neighbours(cell);
            Assert.True(neighbours.Count < 6, cell + " should have fewer than 6 neighbours");
            Assert.All(neighbours, n => Assert.True(map.Contains(n)));
        }

        Assert.Equal(2, map.Neighbours(At(0, 0)).Count);
        Assert.Equal(4, map.Neighbours(At(0, 6)).Count);
    }

    [Fact]
    public void TRN_023_a_position_outside_the_map_is_impassable()
    {
        var map = Sample();
        var edge = At(10, 0);

        var outside = edge.Neighbours().Where(n => !map.Contains(n)).ToList();

        Assert.NotEmpty(outside);
        Assert.All(outside, n => Assert.False(map.IsPassable(n)));
        Assert.False(map.IsPassable(At(-1, 5)));
        Assert.False(map.IsPassable(At(20, 5)));
        Assert.False(map.IsPassable(At(5, 12)));
    }

    [Fact]
    public void TRN_024_mountain_sea_and_river_are_impassable_and_plain_is_passable()
    {
        var map = MapLoader.Load(
            "{\"width\": 5, \"height\": 5, \"default\": \"plain\", \"terrain\": " +
            "{\"mountain\": [[1,1]], \"sea\": [[2,2]], \"river\": [[3,3]]}}");

        Assert.Equal(TerrainType.Mountain, map.TerrainAt(At(1, 1)));
        Assert.Equal(TerrainType.Sea, map.TerrainAt(At(2, 2)));
        Assert.Equal(TerrainType.River, map.TerrainAt(At(3, 3)));
        Assert.False(map.IsPassable(At(1, 1)));
        Assert.False(map.IsPassable(At(2, 2)));
        Assert.False(map.IsPassable(At(3, 3)));
        Assert.True(map.IsPassable(At(0, 0)));
    }

    [Fact]
    public void TRN_024_sample_river_line_must_be_gone_around()
    {
        var map = Sample();
        for (var row = 0; row <= 9; row++)
        {
            Assert.Equal(TerrainType.River, map.TerrainAt(At(9, row)));
        }
        var start = At(5, 5);
        var goal = At(13, 5);

        var steps = ShortestPassableSteps(map, start, goal);

        Assert.NotNull(steps);
        Assert.True(steps > start.DistanceTo(goal), "river should force a longer route than the straight line");
    }

    [Fact]
    public void MAP_019_river_is_column_9_rows_0_to_9_and_rows_10_and_11_are_the_one_gap()
    {
        var map = Sample();

        for (var row = 0; row < map.Height; row++)
        {
            var expected = row <= 9 ? TerrainType.River : TerrainType.Plain;
            Assert.Equal(expected, map.TerrainAt(At(9, row)));
        }
        for (var c = 0; c < map.Width; c++)
        {
            if (c == 9) continue;
            for (var r = 0; r < map.Height; r++)
                Assert.NotEqual(TerrainType.River, map.TerrainAt(At(c, r)));
        }
    }

    [Fact]
    public void MAP_019_the_river_splits_the_players_and_the_only_way_across_is_the_gap()
    {
        var map = Sample();
        var scenario = GameConfigLoader.Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "maps", "sample-scenario.json")));
        var p1 = HexCoord.FromOffset(scenario.Player1Start);
        var p2 = HexCoord.FromOffset(scenario.Player2Start);

        Assert.True(map.IsPassable(p1));
        Assert.True(map.IsPassable(p2));
        Assert.True(scenario.Player1Start.Column < 9, "Player 1 is left of the river");
        Assert.True(scenario.Player2Start.Column > 9, "Player 2 is right of the river");

        var around = ShortestPassableSteps(map, p1, p2);
        Assert.NotNull(around);
        Assert.True(around > p1.DistanceTo(p2), "the way round is longer than the straight line");

        // Close the gap (and nothing else): the two sides are then cut off from each other.
        var gapClosed = new HashSet<HexCoord> { At(9, 10), At(9, 11) };
        Assert.Null(ShortestPassableSteps(map, p1, p2, gapClosed));
    }

    [Fact]
    public void TEC_008_changing_one_map_hex_changes_the_map()
    {
        var a = Sample();
        var cells = new TerrainType[a.Width * a.Height];
        for (var i = 0; i < cells.Length; i++)
            cells[i] = a.TerrainAt(At(i % a.Width, i / a.Width));
        cells[10 * a.Width + 9] = TerrainType.River; // close the gap at (9,10)

        Assert.NotEqual(a, new HexMap(a.Width, a.Height, cells));
    }

    [Fact]
    public void MAP_018_unknown_terrain_fails_naming_the_cell()
    {
        var ex = Assert.Throws<MapLoadException>(() => MapLoader.Load(
            "{\"width\": 5, \"height\": 5, \"default\": \"plain\", \"terrain\": {\"lava\": [[2,3]]}}"));

        Assert.Contains("lava", ex.Message);
        Assert.Contains("col 2, row 3", ex.Message);
    }

    [Fact]
    public void MAP_019_cell_outside_the_stated_size_fails_naming_the_cell()
    {
        var ex = Assert.Throws<MapLoadException>(() => MapLoader.Load(
            "{\"width\": 5, \"height\": 5, \"default\": \"plain\", \"terrain\": {\"sea\": [[1,1],[5,2]]}}"));

        Assert.Contains("col 5, row 2", ex.Message);
    }

    [Fact]
    public void MAP_018_a_cell_given_two_terrains_fails_naming_the_cell()
    {
        var ex = Assert.Throws<MapLoadException>(() => MapLoader.Load(
            "{\"width\": 5, \"height\": 5, \"default\": \"plain\", \"terrain\": {\"sea\": [[1,1]], \"river\": [[1,1]]}}"));

        Assert.Contains("col 1, row 1", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("{\"width\": 5, \"height\": 5}")]
    [InlineData("{\"width\": 0, \"height\": 5, \"default\": \"plain\"}")]
    [InlineData("{\"width\": 5, \"height\": 5, \"default\": \"plain\"")]
    [InlineData("{\"width\": 5, \"height\": 5, \"default\": \"swamp\"}")]
    public void MAP_018_malformed_files_fail_with_a_load_error(string json)
    {
        Assert.Throws<MapLoadException>(() => MapLoader.Load(json));
    }

    private static int? ShortestPassableSteps(HexMap map, HexCoord start, HexCoord goal, ISet<HexCoord>? closed = null)
    {
        var dist = new Dictionary<HexCoord, int> { [start] = 0 };
        var queue = new Queue<HexCoord>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur == goal) return dist[cur];
            foreach (var n in map.Neighbours(cur))
            {
                if (map.IsPassable(n) && (closed == null || !closed.Contains(n)) && !dist.ContainsKey(n))
                {
                    dist[n] = dist[cur] + 1;
                    queue.Enqueue(n);
                }
            }
        }
        return null;
    }
}
