using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class FactionViewTests
{
    private const string Types =
        "{\"version\":1,\"sectionTypes\":{\"a\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    // West HQ (5,4), east HQ (11,7), neutral major-1 (8,1). A mountain at (0,0).
    private const string Map =
        "{\"version\":1,\"width\":12,\"height\":8,\"terrain\":{\"mountain\":[[0,0]]},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[11,7],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[5,4],\"faction\":\"west\"}," +
        "{\"id\":\"major-1\",\"type\":\"major\",\"at\":[8,1]}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"a\",\"a\",\"a\"]}},\"west\":{\"brigade\":{\"sections\":[\"a\",\"a\",\"a\"]}}}}";

    private const string Params =
        "P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 50\nP-RES-06 = 20\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\n" +
        "P-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1";

    private static GameState NewGame() => new GameState(7, MapImporter.Parse(Map), SectionTypeTable.Parse(Types), ParameterTable.Parse(Params));
    private static HexCoord Hex(int c, int r) => HexCoord.FromOffset(c, r);
    private static Brigade Of(GameState g, string f) => g.Brigades.Single(b => b.Faction == f);
    private static FactionView View(GameState g, string f) => g.FogOfWar!.ViewFor(g, f);
    private static void Do(GameState g, ICommand c) => Assert.True(g.ApplyCommand(c).Accepted);
    private static void EndTurn(GameState g) => Do(g, new EndTurnCommand(g.ActiveFaction!));

    private static void MoveTo(GameState g, string faction, HexCoord target)
    {
        var b = Of(g, faction);
        var path = new List<HexCoord>();
        var at = b.Position;
        while (at != target)
        {
            at = at.Neighbours().OrderBy(n => n.DistanceTo(target)).First();
            path.Add(at);
        }
        Do(g, new MoveBrigadeCommand(faction, b.Id, path));
    }

    // Seed 7 gives west the first turn.
    [Fact]
    public void FOW_002_the_view_holds_every_hex_with_its_terrain_even_far_from_every_brigade()
    {
        var g = NewGame();
        var hash = g.ComputeHash();
        var v = View(g, "west");
        Assert.Equal(12 * 8, v.Terrain.Count);
        Assert.Equal(TerrainType.Mountain, v.Terrain.Single(h => h.Position == Hex(0, 0)).Terrain);
        Assert.Equal(TerrainType.Open, v.Terrain.Single(h => h.Position == Hex(11, 0)).Terrain);
        Assert.Equal(hash, g.ComputeHash());
    }

    [Fact]
    public void FOW_003_an_enemy_brigade_is_in_the_view_only_while_in_sight_with_its_faction_and_hex()
    {
        var g = NewGame();
        MoveTo(g, "west", Hex(7, 1));
        Assert.Empty(View(g, "west").EnemyBrigades);

        EndTurn(g);
        MoveTo(g, "east", Hex(8, 1));
        var seen = View(g, "west").EnemyBrigades.Single();
        Assert.Equal("east", seen.Faction);
        Assert.Equal(Hex(8, 1), seen.Position);
    }

    [Fact]
    public void FOW_003_a_never_seen_neutral_or_enemy_st_is_not_in_the_view()
    {
        var v = View(NewGame(), "west");
        Assert.DoesNotContain(v.Locations, l => l.Id == "major-1");
        Assert.DoesNotContain(v.Locations, l => l.Id == "hq-east");
    }

    [Fact]
    public void FOW_010_FOW_009_a_seen_location_out_of_sight_shows_its_last_known_owner_not_the_true_one()
    {
        var g = NewGame();
        MoveTo(g, "west", Hex(7, 1));
        var inSight = View(g, "west").Locations.Single(l => l.Id == "major-1");
        Assert.True(inSight.InSightNow);
        Assert.Null(inSight.Owner);

        EndTurn(g);
        EndTurn(g);
        MoveTo(g, "west", Hex(5, 4));
        g.Locations.Single(l => l.Id == "major-1").Owner = "east";
        var remembered = View(g, "west").Locations.Single(l => l.Id == "major-1");
        Assert.False(remembered.InSightNow);
        Assert.Null(remembered.Owner);
        Assert.Equal(Hex(8, 1), remembered.Position);
    }

    [Fact]
    public void FOW_003_own_brigades_and_owned_locations_are_always_in_the_view_with_own_reserve_and_cp()
    {
        var g = NewGame();
        var v = View(g, "west");
        var b = Of(g, "west");
        var own = v.OwnBrigades.Single();
        Assert.Equal(b.Id, own.Id);
        Assert.Equal(b.SP, own.SP);
        Assert.Equal(b.Position, own.Position);
        Assert.Equal(3, own.Sections.Count);
        Assert.Contains(v.Locations, l => l.Id == "hq-west" && l.Owner == "west" && l.InSightNow);
        var f = g.Factions.Single(x => x.Id == "west");
        Assert.Equal(f.Reserve, v.Reserve);
        Assert.Equal(f.Cp, v.Cp);
    }

    [Fact]
    public void FOW_001_two_factions_views_differ_and_each_lacks_what_only_the_other_sees()
    {
        var g = NewGame();
        MoveTo(g, "west", Hex(7, 1));
        var west = View(g, "west");
        var east = View(g, "east");
        Assert.Contains(west.Locations, l => l.Id == "major-1");
        Assert.DoesNotContain(east.Locations, l => l.Id == "major-1");
        Assert.Contains(east.Locations, l => l.Id == "hq-east");
        Assert.DoesNotContain(west.Locations, l => l.Id == "hq-east");
        Assert.NotEqual(west.OwnBrigades.Single().Position, east.OwnBrigades.Single().Position);
    }

    [Fact]
    public void MUL_014_TEC_008_a_made_view_does_not_change_when_the_state_changes_and_making_it_leaves_the_hash()
    {
        var g = NewGame();
        var before = g.ComputeHash();
        var v = View(g, "west");
        Assert.Equal(before, g.ComputeHash());

        var pos = v.OwnBrigades.Single().Position;
        var count = v.Locations.Count;
        MoveTo(g, "west", Hex(7, 1));
        Assert.Equal(pos, v.OwnBrigades.Single().Position);
        Assert.Equal(count, v.Locations.Count);
        Assert.NotEqual(pos, Of(g, "west").Position);
    }
}
