using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class KnownLocationsTests
{
    private const string Types =
        "{\"version\":1,\"sectionTypes\":{\"a\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    // West HQ (5,4), east HQ (11,7), neutral major-1 (8,1): 5 hexes from west HQ, so outside its HQ radius of 3.
    private const string Map =
        "{\"version\":1,\"width\":12,\"height\":8,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
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
    private static FactionState Fac(GameState g, string f) => g.Factions.Single(x => x.Id == f);
    private static LocationState Loc(GameState g, string id) => g.Locations.Single(l => l.Id == id);
    private static KnownLocation? Known(GameState g, string f, string id) => Fac(g, f).KnownLocations.TryGetValue(id, out var k) ? k : null;
    private static string KnownText(GameState g, string f) => string.Join("\n", g.CanonicalText().Split('\n').Where(l => l.StartsWith("known=" + f + " ")));

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
    private static GameState WestNextToMajor()
    {
        var g = NewGame();
        MoveTo(g, "west", Hex(7, 1));
        return g;
    }

    [Fact]
    public void FOW_009_a_neutral_st_that_comes_into_sight_is_recorded_as_neutral()
    {
        var g = NewGame();
        Assert.Null(Known(g, "west", "major-1"));

        MoveTo(g, "west", Hex(7, 1));

        var k = Known(g, "west", "major-1");
        Assert.NotNull(k);
        Assert.Null(k!.Owner);
        Assert.Equal(LocationType.Major, k.Type);
    }

    [Fact]
    public void FOW_009_a_spotted_enemy_st_stays_with_its_last_known_owner_once_out_of_sight()
    {
        var g = NewGame();
        Loc(g, "major-1").Owner = "east";
        MoveTo(g, "west", Hex(7, 1));
        Assert.Equal("east", Known(g, "west", "major-1")!.Owner);

        EndTurn(g);
        EndTurn(g);
        MoveTo(g, "west", Hex(5, 4));

        Assert.DoesNotContain(Hex(8, 1), new FogOfWar(g.SectionTypes!).VisibleHexes(g, "west"));
        Assert.Equal("east", Known(g, "west", "major-1")!.Owner);
    }

    [Fact]
    public void FOW_009_MUL_014_an_owner_change_out_of_sight_leaves_the_record_until_the_st_is_seen_again()
    {
        var g = WestNextToMajor();
        EndTurn(g);
        EndTurn(g);
        MoveTo(g, "west", Hex(5, 4));
        var before = KnownText(g, "west");

        Loc(g, "major-1").Owner = "east"; // east captures it while west cannot see
        Do(g, new AddToTallyCommand(1, "west"));

        Assert.Null(Known(g, "west", "major-1")!.Owner);
        Assert.Equal(before, KnownText(g, "west"));

        EndTurn(g);
        EndTurn(g);
        MoveTo(g, "west", Hex(7, 1));
        Assert.Equal("east", Known(g, "west", "major-1")!.Owner);
    }

    [Fact]
    public void MUL_014_what_one_faction_sees_is_not_added_to_another()
    {
        var g = WestNextToMajor();

        Assert.NotNull(Known(g, "west", "major-1"));
        Assert.Null(Known(g, "east", "major-1"));
        Assert.Null(Known(g, "east", "hq-west"));
    }

    [Fact]
    public void FOW_010_an_enemy_fob_is_recorded_only_once_in_sight_with_the_type_seen_then()
    {
        var g = NewGame();
        var major = Loc(g, "major-1");
        major.Owner = "east";
        major.Upgraded = true;
        Do(g, new AddToTallyCommand(1, "west"));
        Assert.Null(Known(g, "west", "major-1"));

        MoveTo(g, "west", Hex(7, 1));

        Assert.Equal(LocationType.Fob, Known(g, "west", "major-1")!.Type);
        Assert.Equal("east", Known(g, "west", "major-1")!.Owner);
    }

    [Fact]
    public void FOW_010_an_enemy_hq_is_recorded_only_once_in_sight()
    {
        var g = NewGame();
        Assert.Null(Known(g, "west", "hq-east"));

        Of(g, "west").Position = Hex(11, 6);
        Do(g, new AddToTallyCommand(1, "west"));

        var k = Known(g, "west", "hq-east");
        Assert.Equal("east", k!.Owner);
        Assert.Equal(LocationType.Hq, k.Type);
    }

    [Fact]
    public void FOW_009_an_upgrade_out_of_sight_does_not_change_the_recorded_type()
    {
        var g = WestNextToMajor();
        EndTurn(g);
        EndTurn(g);
        MoveTo(g, "west", Hex(5, 4));

        Loc(g, "major-1").Upgraded = true;
        Do(g, new AddToTallyCommand(1, "west"));

        Assert.Equal(LocationType.Major, Known(g, "west", "major-1")!.Type);
    }

    [Fact]
    public void FOW_003_an_st_that_was_never_in_sight_has_no_record()
    {
        var g = NewGame();
        EndTurn(g);
        EndTurn(g);

        Assert.Null(Known(g, "west", "major-1"));
        Assert.Null(Known(g, "west", "hq-east"));
    }

    [Fact]
    public void FOW_009_a_new_game_records_only_what_each_faction_sees_from_its_own_hq()
    {
        var g = NewGame();

        Assert.Equal(new[] { "hq-west" }, Fac(g, "west").KnownLocations.Keys.OrderBy(k => k));
        Assert.Equal(new[] { "hq-east" }, Fac(g, "east").KnownLocations.Keys.OrderBy(k => k));
        Assert.Equal("west", Known(g, "west", "hq-west")!.Owner);
    }

    [Fact]
    public void FOW_009_a_capture_that_completes_in_sight_at_the_start_of_a_turn_is_recorded()
    {
        var g = NewGame();
        MoveTo(g, "west", Hex(8, 1));
        Assert.Null(Known(g, "west", "major-1")!.Owner);
        EndTurn(g); // west ends on the ST: capture pending
        EndTurn(g); // east ends; west's turn starts and the capture completes

        Assert.Equal("west", Loc(g, "major-1").Owner);
        Assert.Equal("west", Known(g, "west", "major-1")!.Owner);
    }

    [Fact]
    public void TEC_008_TEC_019_the_record_is_in_the_hash_as_integers_and_text_only()
    {
        var a = NewGame();
        var b = NewGame();
        Assert.Equal(a.ComputeHash(), b.ComputeHash());

        Fac(b, "west").Remember("major-1", null, LocationType.Major);

        Assert.NotEqual(a.ComputeHash(), b.ComputeHash());
        Assert.Contains("known=west hq-west owner=west type=Hq\n", a.CanonicalText());
        Assert.Contains("known=west major-1 owner=- type=Major\n", b.CanonicalText());
        Assert.All(b.CanonicalText().Split('\n').Where(l => l.StartsWith("known=")), l => Assert.DoesNotContain(".", l));
    }

    [Fact]
    public void TEC_008_replay_reproduces_the_record()
    {
        var g = WestNextToMajor();
        EndTurn(g);
        EndTurn(g);
        MoveTo(g, "west", Hex(5, 4));

        var replayed = GameState.Replay(7, g.Log, MapImporter.Parse(Map), SectionTypeTable.Parse(Types), ParameterTable.Parse(Params));

        Assert.Equal(g.CanonicalText(), replayed.CanonicalText());
        Assert.NotNull(Known(replayed, "west", "major-1"));
    }
}
