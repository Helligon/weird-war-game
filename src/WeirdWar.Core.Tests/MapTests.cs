using WeirdWar.Core.Maps;

namespace WeirdWar.Core.Tests;

public class MapTests
{
    private static string Sample(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "maps", name));

    private static string Map(string terrain, int w = 6, int h = 4)
        => "{\"version\":1,\"width\":" + w + ",\"height\":" + h + ",\"terrain\":" + terrain + "}";

    private static HexCoord At(int c, int r) => HexCoord.FromOffset(c, r);

    // Independent odd-q neighbour table (column offset, row offset) per column parity.
    private static readonly (int, int)[] EvenCol = { (1, -1), (1, 0), (0, 1), (-1, 0), (-1, -1), (0, -1) };
    private static readonly (int, int)[] OddCol = { (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (0, -1) };

    private static HashSet<(int, int)> OffsetSet(IEnumerable<HexCoord> hexes)
        => hexes.Select(h => h.ToOffset()).ToHashSet();

    // ---- MAP-017: hex grid, 6 neighbours except at the edge

    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 0, 1)]
    [InlineData(0, 0, 2, 0, 2)]
    [InlineData(0, 0, 0, 5, 5)]
    [InlineData(1, 0, 0, 1, 1)]
    [InlineData(1, 0, 0, 0, 1)]
    [InlineData(0, 0, 19, 11, 21)]
    [InlineData(3, 2, 7, 6, 6)]
    public void MAP_017_hex_distance_pinned_cases(int c1, int r1, int c2, int r2, int expected)
    {
        Assert.Equal(expected, At(c1, r1).DistanceTo(At(c2, r2)));
        Assert.Equal(expected, At(c2, r2).DistanceTo(At(c1, r1)));
    }

    [Theory]
    [InlineData(4, 5)]
    [InlineData(5, 5)]
    public void MAP_017_neighbours_match_odd_q_table(int c, int r)
    {
        var table = c % 2 == 0 ? EvenCol : OddCol;
        var expected = table.Select(d => (c + d.Item1, r + d.Item2)).ToHashSet();

        var actual = OffsetSet(At(c, r).Neighbours());

        Assert.Equal(6, At(c, r).Neighbours().Length);
        Assert.Equal(expected, actual);
        Assert.All(At(c, r).Neighbours(), n => Assert.Equal(1, n.DistanceTo(At(c, r))));
    }

    [Fact]
    public void MAP_017_offset_and_axial_round_trip()
    {
        for (var c = 0; c < 50; c++)
            for (var r = 0; r < 30; r++)
                Assert.Equal((c, r), HexCoord.FromOffset(c, r).ToOffset());
    }

    [Fact]
    public void MAP_017_interior_hexes_have_six_neighbours_and_edge_hexes_fewer()
    {
        var map = MapImporter.Parse(Sample("sample-20x12.json"));

        for (var c = 1; c < map.Width - 1; c++)
            for (var r = 1; r < map.Height - 1; r++)
                Assert.Equal(6, map.Neighbours(At(c, r)).Count);

        Assert.Equal(2, map.Neighbours(At(0, 0)).Count);   // even top-left corner
        Assert.Equal(4, map.Neighbours(At(0, 5)).Count);   // left edge
        Assert.Equal(3, map.Neighbours(At(4, 0)).Count);   // top edge, even column
        Assert.Equal(5, map.Neighbours(At(5, 0)).Count);   // top edge, odd column
    }

    // ---- MAP-018: fixed, same file same map

    [Fact]
    public void MAP_018_same_file_always_gives_the_same_map()
    {
        var text = Sample("sample-20x12.json");

        Assert.Equal(MapImporter.Parse(text).CanonicalText(), MapImporter.Parse(text).CanonicalText());
    }

    [Fact]
    public void MAP_018_canonical_text_is_pinned_for_a_tiny_map()
    {
        var map = MapImporter.Parse(Map("{\"mountain\":[[1,1]],\"sea\":[[0,0]],\"river\":[[2,0],[2,1]]}", 3, 2));

        Assert.Equal("3x2\nS.R\n.MR", map.CanonicalText());
    }

    // ---- MAP-019, P-MAP-04: size comes from the file

    [Fact]
    public void MAP_019_P_MAP_04_sample_50_by_30_imports_at_that_size()
    {
        var map = MapImporter.Parse(Sample("sample-50x30.json"));

        Assert.Equal(50, map.Width);
        Assert.Equal(30, map.Height);
        Assert.Equal(TerrainType.Sea, map.TerrainAt(0, 0));
        Assert.Equal(TerrainType.River, map.TerrainAt(24, 5));
        Assert.Equal(TerrainType.Open, map.TerrainAt(24, 12));
        Assert.Equal(TerrainType.Open, map.TerrainAt(49, 20));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(20, 12)]
    [InlineData(7, 3)]
    public void MAP_019_smaller_maps_also_import(int w, int h)
    {
        var map = MapImporter.Parse(Map("{}", w, h));

        Assert.Equal((w, h), (map.Width, map.Height));
    }

    [Fact]
    public void MAP_019_sample_20x12_imports()
    {
        var map = MapImporter.Parse(Sample("sample-20x12.json"));

        Assert.Equal((20, 12), (map.Width, map.Height));
    }

    // ---- MED-004: documented format accepted without an editor; malformed input fails clearly

    [Fact]
    public void MED_004_a_hand_written_file_in_the_documented_format_imports()
    {
        var map = MapImporter.Parse("{\n \"version\": 1, \"width\": 6, \"height\": 4,\n \"terrain\": {\"mountain\": [[1,1],[2,1]], \"sea\": [[0,0]], \"river\": [[4,0],[4,1],[4,2],[4,3]]}\n}");

        Assert.Equal(TerrainType.Mountain, map.TerrainAt(2, 1));
        Assert.Equal(TerrainType.River, map.TerrainAt(4, 3));
    }

    [Fact]
    public void MED_004_terrain_is_optional_and_means_all_open()
    {
        var map = MapImporter.Parse("{\"version\":1,\"width\":2,\"height\":2}");

        Assert.Equal("2x2\n..\n..", map.CanonicalText());
    }

    public static TheoryData<string, string> BadMaps => new()
    {
        { "{\"version\":1,\"width\":3", "not valid JSON" },
        { "", "not valid JSON" },
        { "[1,2]", "must be a JSON object" },
        { Map("{\"lava\":[[0,0]]}"), "Unknown terrain type \"lava\"" },
        { Map("{\"sea\":[[6,0]]}"), "outside the 6x4 grid" },
        { Map("{\"sea\":[[0,4]]}"), "outside the 6x4 grid" },
        { Map("{\"sea\":[[-1,0]]}"), "outside the 6x4 grid" },
        { Map("{\"sea\":[[1,1]],\"river\":[[1,1]]}"), "more than once" },
        { Map("{\"sea\":[[1]]}"), "[column,row]" },
        { Map("{\"sea\":[[1.5,0]]}"), "[column,row]" },
        { Map("{\"sea\":[\"a\"]}"), "[column,row]" },
        { Map("{\"sea\":5}"), "list of" },
        { Map("[]"), "\"terrain\" must be an object" },
        { "{\"version\":1,\"height\":4}", "Missing required key \"width\"" },
        { "{\"width\":4,\"height\":4}", "Missing required key \"version\"" },
        { "{\"version\":1,\"width\":\"4\",\"height\":4}", "\"width\" must be a whole number" },
        { "{\"version\":1,\"width\":0,\"height\":4}", "\"width\" must be between" },
        { "{\"version\":1,\"width\":4,\"height\":-2}", "\"height\" must be between" },
        { "{\"version\":1,\"width\":100000,\"height\":4}", "\"width\" must be between" },
    };

    [Theory]
    [MemberData(nameof(BadMaps))]
    public void MED_004_malformed_maps_fail_with_a_clear_error_and_no_map(string json, string expectedPart)
    {
        var e = Assert.Throws<MapFormatException>(() => MapImporter.Parse(json));

        Assert.Contains(expectedPart, e.Message);
    }

    [Fact]
    public void MED_004_json_syntax_errors_name_line_and_column()
    {
        var e = Assert.Throws<MapFormatException>(() => MapImporter.Parse("{\n  \"version\": 1,\n  oops\n}"));

        Assert.Contains("line 3", e.Message);
    }

    // ---- MED-006: versioned, unknown keys are errors (room for CC1-68)

    [Fact]
    public void MED_006_unknown_top_level_key_is_a_clear_error()
    {
        var e = Assert.Throws<MapFormatException>(() =>
            MapImporter.Parse("{\"version\":1,\"width\":2,\"height\":2,\"locations\":[]}"));

        Assert.Contains("Unknown key \"locations\"", e.Message);
    }

    [Fact]
    public void MED_006_unsupported_version_is_a_clear_error()
    {
        var e = Assert.Throws<MapFormatException>(() =>
            MapImporter.Parse("{\"version\":2,\"width\":2,\"height\":2}"));

        Assert.Contains("Unsupported map version 2", e.Message);
    }

    [Fact]
    public void MED_006_format_documentation_exists_next_to_the_code_and_lists_fields_and_terrain()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "WeirdWar.Core", "Maps", "MapFormat.md")))
            dir = dir.Parent;
        Assert.NotNull(dir);

        var doc = File.ReadAllText(Path.Combine(dir!.FullName, "WeirdWar.Core", "Maps", "MapFormat.md"));

        foreach (var word in new[] { "`version`", "`width`", "`height`", "`terrain`", "`open`", "`mountain`", "`sea`", "`river`" })
            Assert.Contains(word, doc);
    }

    // ---- MED-006, TRN-024: terrain and passability

    [Fact]
    public void TRN_024_mountain_sea_and_river_are_impassable_and_open_has_equal_cost()
    {
        var map = MapImporter.Parse(Sample("sample-20x12.json"));

        Assert.Equal(TerrainType.Mountain, map.TerrainAt(4, 1));
        Assert.Equal(TerrainType.Sea, map.TerrainAt(0, 0));
        Assert.Equal(TerrainType.River, map.TerrainAt(9, 3));
        foreach (var (c, r) in new[] { (4, 1), (0, 0), (9, 3) })
        {
            Assert.False(map.IsPassable(At(c, r)));
            Assert.Null(map.MovementCost(At(c, r)));
        }

        var openCosts = new HashSet<int?>();
        var openCount = 0;
        for (var c = 0; c < map.Width; c++)
            for (var r = 0; r < map.Height; r++)
                if (map.TerrainAt(c, r) == TerrainType.Open)
                {
                    openCount++;
                    Assert.True(map.IsPassable(At(c, r)));
                    openCosts.Add(map.MovementCost(At(c, r)));
                }
        Assert.True(openCount > 0);
        Assert.Single(openCosts);
    }

    [Fact]
    public void TRN_024_hexes_off_the_map_are_not_passable()
    {
        var map = MapImporter.Parse(Map("{}"));

        Assert.False(map.IsPassable(At(-1, 0)));
        Assert.False(map.IsPassable(At(6, 0)));
        Assert.Null(map.MovementCost(At(0, 4)));
    }

    [Fact]
    public void MED_006_open_can_be_listed_explicitly_and_unlisted_hexes_are_open()
    {
        var map = MapImporter.Parse(Map("{\"open\":[[0,0]]}", 2, 1));

        Assert.Equal("2x1\n..", map.CanonicalText());
    }
}
