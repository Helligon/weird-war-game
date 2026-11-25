using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Core.Tests;

public class MapTests
{
    private static string SampleJson() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "maps", "sample.json"));

    private static HexMap Sample() => MapLoader.Load(SampleJson());

    private static HexCoord At(int col, int row) => HexCoord.FromOffset(new OffsetCoord(col, row));

    [Fact]
    public void MAP_019_sample_map_is_30_columns_by_50_rows_and_every_cell_has_terrain()
    {
        var map = Sample();

        Assert.Equal(30, map.Width);
        Assert.Equal(50, map.Height);
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

        Assert.Equal(6, map.Neighbours(At(10, 20)).Count);
    }

    [Fact]
    public void MAP_017_edge_and_corner_cells_have_fewer_than_six_neighbours_all_inside_the_map()
    {
        var map = Sample();

        foreach (var cell in new[] { At(0, 0), At(29, 49), At(10, 0), At(0, 20), At(29, 20), At(15, 49) })
        {
            var neighbours = map.Neighbours(cell);
            Assert.True(neighbours.Count < 6, cell + " should have fewer than 6 neighbours");
            Assert.All(neighbours, n => Assert.True(map.Contains(n)));
        }
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
        Assert.False(map.IsPassable(At(30, 5)));
        Assert.False(map.IsPassable(At(5, 50)));
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
        var start = At(10, 5);
        var goal = At(20, 5);

        var steps = ShortestPassableSteps(map, start, goal);

        Assert.NotNull(steps);
        Assert.True(steps > start.DistanceTo(goal), "river should force a longer route than the straight line");
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

    private static int? ShortestPassableSteps(HexMap map, HexCoord start, HexCoord goal)
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
                if (map.IsPassable(n) && !dist.ContainsKey(n))
                {
                    dist[n] = dist[cur] + 1;
                    queue.Enqueue(n);
                }
            }
        }
        return null;
    }
}
