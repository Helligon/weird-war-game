using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class VisibleHexesTests
{
    private static string Types(int sight) =>
        "{\"version\":1,\"sectionTypes\":{\"a\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":" + sight + ",\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    // West HQ at offset (5,4), east HQ at (11,7), neutral major-1 at (8,1). A mountain can be added.
    private static string Map(string mountains = "") =>
        "{\"version\":1,\"width\":12,\"height\":8,\"terrain\":{" + (mountains.Length > 0 ? "\"mountain\":[" + mountains + "]" : "") + "},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[11,7],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[5,4],\"faction\":\"west\"}," +
        "{\"id\":\"major-1\",\"type\":\"major\",\"at\":[8,1]}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"a\",\"a\",\"a\"]}},\"west\":{\"brigade\":{\"sections\":[\"a\",\"a\",\"a\"]}}}}";

    private static string Params(string sight = "hq=3 fob=2 major=2 command=1 supply=1") =>
        "P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 50\nP-RES-06 = 20\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\nP-FOW-03 = " + sight + "\n" +
        "P-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1";

    private static GameState NewGame(int sight = 1, string sightParam = "hq=3 fob=2 major=2 command=1 supply=1", string mountains = "")
        => new GameState(7, MapImporter.Parse(Map(mountains)), SectionTypeTable.Parse(Types(sight)), ParameterTable.Parse(Params(sightParam)));

    private static FogOfWar Fog(GameState g) => new FogOfWar(g.SectionTypes!);
    private static HexCoord Hex(int c, int r) => HexCoord.FromOffset(c, r);
    private static IReadOnlyList<HexCoord> West(GameState g) => Fog(g).VisibleHexes(g, "west");

    private static void AssertAreaAround(GameState g, HexCoord centre, int radius, IEnumerable<HexCoord> actual)
    {
        var expected = new List<HexCoord>();
        for (var c = 0; c < g.Map!.Width; c++)
            for (var r = 0; r < g.Map.Height; r++)
                if (Hex(c, r).DistanceTo(centre) <= radius) expected.Add(Hex(c, r));
        Assert.Equal(expected.OrderBy(h => h.Q).ThenBy(h => h.R), actual);
    }

    [Fact]
    public void FOW_006_FOW_004_a_brigade_sees_its_sight_distance_and_no_further()
    {
        var game = NewGame(sight: 2, sightParam: "hq=1 fob=1 major=1 command=1 supply=1");
        var visible = West(game);
        var centre = Hex(5, 4);

        Assert.All(visible, h => Assert.True(h.DistanceTo(centre) <= 2));
        AssertAreaAround(game, centre, 2, visible);
        Assert.DoesNotContain(centre.Neighbours().SelectMany(n => n.Neighbours()).SelectMany(n => n.Neighbours()).First(h => h.DistanceTo(centre) == 3), visible);
    }

    [Fact]
    public void FOW_007_P_FOW_03_an_owned_hq_sees_within_3()
    {
        var game = NewGame(sight: 1);

        AssertAreaAround(game, Hex(5, 4), 3, West(game));
    }

    [Fact]
    public void FOW_007_a_neutral_st_gives_no_sight()
    {
        var game = NewGame(sight: 1);

        Assert.Null(game.Locations.Single(l => l.Id == "major-1").Owner);
        Assert.DoesNotContain(Hex(8, 1), West(game));
        Assert.Empty(Fog(game).VisibleHexes(game, "nobody"));
    }

    [Fact]
    public void FOW_006_FOW_007_overlapping_areas_list_each_hex_once_sorted_by_q_then_r()
    {
        var game = NewGame(sight: 3);
        game.Locations.Single(l => l.Id == "major-1").Owner = "west"; // radius 2 at (8,1) overlaps the Brigade and HQ areas
        var visible = West(game);

        Assert.Equal(visible.Count, visible.Distinct().Count());
        Assert.Equal(visible.OrderBy(h => h.Q).ThenBy(h => h.R), visible);
        Assert.Contains(Hex(8, 1), visible);
        Assert.Contains(Hex(5, 4), visible);
    }

    [Fact]
    public void FOW_007_only_on_map_hexes_are_returned_near_the_edge()
    {
        var game = NewGame(sight: 3);
        var east = Fog(game).VisibleHexes(game, "east"); // east HQ and Brigade sit on the corner (11,7)

        Assert.All(east, h => Assert.True(game.Map!.Contains(h)));
        Assert.Contains(Hex(11, 7), east);
        Assert.True(east.Count < 37); // a full radius-3 area is 37 hexes; the edge clips it
    }

    [Fact]
    public void FOW_003_an_enemy_brigade_adds_nothing()
    {
        var game = NewGame(sight: 3);
        var before = West(game);
        game.Brigades.Single(b => b.Faction == "east").Position = Hex(5, 5);

        Assert.Equal(before, West(game));
        AssertAreaAround(game, Hex(5, 5), 3, Fog(game).VisibleHexes(game, "east").Where(h => h.DistanceTo(Hex(5, 5)) <= 3));
    }

    [Fact]
    public void MUL_014_TEC_008_asking_for_visible_hexes_leaves_the_state_hash_unchanged()
    {
        var game = NewGame(sight: 2);
        var before = game.ComputeHash();

        West(game);

        Assert.Equal(before, game.ComputeHash());
    }

    [Fact]
    public void FOW_002_gap_4_a_mountain_does_not_block_sight()
    {
        var centre = Hex(5, 4);
        var near = centre.Neighbours()[0];
        var far = new HexCoord(near.Q + (near.Q - centre.Q), near.R + (near.R - centre.R));
        var (mc, mr) = near.ToOffset();
        var game = NewGame(sight: 2, sightParam: "hq=1 fob=1 major=1 command=1 supply=1", mountains: "[" + mc + "," + mr + "]");

        Assert.Equal(TerrainType.Mountain, game.Map!.TerrainAt(near));
        Assert.Equal(2, far.DistanceTo(centre));
        Assert.Contains(far, West(game));
    }
}
