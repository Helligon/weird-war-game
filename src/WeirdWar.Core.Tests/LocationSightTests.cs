using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class LocationSightTests
{
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{\"infantry\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    // West HQ (0,0), supply-1 (0,2), command-1 (7,4), major-1 (9,1). Seed 7 gives west the first turn.
    private const string MapJson =
        "{\"version\":1,\"width\":12,\"height\":8,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[11,7],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,0],\"faction\":\"west\"}," +
        "{\"id\":\"supply-1\",\"type\":\"supply\",\"at\":[0,2]}," +
        "{\"id\":\"command-1\",\"type\":\"command\",\"at\":[7,4]}," +
        "{\"id\":\"major-1\",\"type\":\"major\",\"at\":[9,1]}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}}";

    private const string Sight = "P-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1";
    private static string Params(string sight = Sight)
        => "P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 50\nP-RES-06 = 20\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\n" + (sight.Length > 0 ? sight + "\n" : "") +
           "P-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1";

    private static GameState NewGame(string? parameters = null)
        => new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(parameters ?? Params()));

    private static LocationState Loc(GameState g, string id) => g.Locations.Single(l => l.Id == id);
    private static int Radius(GameState g, string id) => g.LocationSight!.RadiusOf(Loc(g, id));

    [Fact]
    public void FOW_007_P_FOW_03_owned_locations_give_the_radius_of_their_type()
    {
        var game = NewGame();
        foreach (var id in new[] { "supply-1", "command-1", "major-1" }) Loc(game, id).Owner = "west";

        Assert.Equal(3, Radius(game, "hq-west"));
        Assert.Equal(2, Radius(game, "major-1"));
        Assert.Equal(1, Radius(game, "command-1"));
        Assert.Equal(1, Radius(game, "supply-1"));
    }

    [Fact]
    public void FOW_007_a_supply_st_upgraded_to_an_fob_uses_the_fob_value()
    {
        var game = NewGame();
        Loc(game, "supply-1").Owner = "west";
        Assert.True(game.ApplyCommand(new UpgradeFobCommand("west", "supply-1")).Accepted);

        Assert.Equal(2, Radius(game, "supply-1"));
    }

    [Fact]
    public void FOW_007_a_neutral_st_gives_no_sight_to_anyone()
    {
        var game = NewGame();

        Assert.Null(Loc(game, "major-1").Owner);
        Assert.Equal(0, Radius(game, "major-1"));
        Assert.Equal(0, Radius(game, "supply-1"));
    }

    // A one-row map: west HQ (0,0), a Supply ST at (2,0), east HQ (4,0). With hq=1 only the ST reaches hex (2,0) once the Brigades are home.
    private const string RowMapJson =
        "{\"version\":1,\"width\":5,\"height\":1,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[4,0],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,0],\"faction\":\"west\"}," +
        "{\"id\":\"supply-1\",\"type\":\"supply\",\"at\":[2,0]}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}}";

    private static void Step(GameState g, ICommand c) => Assert.True(g.ApplyCommand(c).Accepted);

    [Fact]
    public void FOW_007_sight_goes_to_the_new_owner_when_an_st_changes_owner_by_capture()
    {
        var game = new GameState(7, MapImporter.Parse(RowMapJson), SectionTypeTable.Parse(TypesJson),
            ParameterTable.Parse(Params("P-FOW-03 = hq=1 fob=2 major=2 command=1 supply=1")));
        var fog = new FogOfWar(game.SectionTypes!);
        var probe = HexCoord.FromOffset(2, 0);
        Assert.DoesNotContain(probe, fog.VisibleHexes(game, "west"));
        Assert.DoesNotContain(probe, fog.VisibleHexes(game, "east"));

        Step(game, new MoveBrigadeCommand("west", 2, new[] { HexCoord.FromOffset(1, 0), probe }));
        Step(game, new EndTurnCommand("west"));
        Step(game, new EndTurnCommand("east")); // west's capture lands
        Assert.Equal("west", Loc(game, "supply-1").Owner);
        Step(game, new MoveBrigadeCommand("west", 2, new[] { HexCoord.FromOffset(1, 0), HexCoord.FromOffset(0, 0) }));
        Assert.Contains(probe, fog.VisibleHexes(game, "west"));
        Assert.DoesNotContain(probe, fog.VisibleHexes(game, "east"));
        Step(game, new EndTurnCommand("west"));

        Step(game, new MoveBrigadeCommand("east", 1, new[] { HexCoord.FromOffset(3, 0), probe }));
        Step(game, new EndTurnCommand("east"));
        Step(game, new EndTurnCommand("west"));
        Step(game, new MoveBrigadeCommand("east", 1, new[] { HexCoord.FromOffset(3, 0), HexCoord.FromOffset(4, 0) })); // east's capture has landed
        Assert.Equal("east", Loc(game, "supply-1").Owner);
        Assert.Contains(probe, fog.VisibleHexes(game, "east"));
        Assert.DoesNotContain(probe, fog.VisibleHexes(game, "west")); // the old owner loses sight
    }

    [Fact]
    public void FOW_007_an_st_with_its_owner_cleared_gives_no_sight()
    {
        var game = NewGame();
        var st = Loc(game, "supply-1");
        st.Owner = "west";
        Assert.Equal(1, game.LocationSight!.RadiusOf(st));
        st.Owner = null;
        Assert.Equal(0, game.LocationSight.RadiusOf(st));
    }

    [Fact]
    public void FOW_007_asking_for_sight_changes_no_state()
    {
        var game = NewGame();
        var before = game.ComputeHash();

        Radius(game, "hq-west");

        Assert.Equal(before, game.ComputeHash());
    }

    [Fact]
    public void FOW_007_P_FOW_03_DLV_014_the_radius_follows_the_parameter_file()
    {
        var game = NewGame(Params("P-FOW-03 = hq=5 fob=4 major=3 command=2 supply=2"));

        Assert.Equal(5, Radius(game, "hq-west"));
        game.Locations.Single(l => l.Id == "major-1").Owner = "west";
        game.Locations.Single(l => l.Id == "command-1").Owner = "west";
        game.Locations.Single(l => l.Id == "supply-1").Owner = "west";
        Assert.Equal(3, Radius(game, "major-1"));
        Assert.Equal(2, Radius(game, "command-1"));
        Assert.Equal(2, Radius(game, "supply-1"));
        Assert.True(game.ApplyCommand(new UpgradeFobCommand("west", "supply-1")).Accepted);
        Assert.Equal(4, Radius(game, "supply-1")); // fob=4
    }

    [Fact]
    public void FOW_007_P_FOW_03_DLV_014_a_missing_parameter_fails_at_game_start()
    {
        Assert.Throws<MissingParameterException>(() => NewGame(Params("")));
    }

    [Fact]
    public void FOW_007_P_FOW_03_DLV_014_a_malformed_parameter_fails_at_game_start()
    {
        Assert.Throws<ParameterFormatException>(() => NewGame(Params("P-FOW-03 = hq=3 fob=2 major=2 command=1")));
    }
}
