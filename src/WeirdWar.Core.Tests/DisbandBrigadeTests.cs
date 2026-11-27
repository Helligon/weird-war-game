using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class DisbandBrigadeTests
{
    // Same fixture as CaptureTerritoryTests. 12 columns x 8 rows. West HQ (0,0) in the corner, east HQ (11,7). supply-1 (0,2), command-1 (7,4), major-1 (9,1).
    // Seed 7 gives west the first turn (see TurnTests). Brigade 1 is east, brigade 2 is west.
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{" +
        "\"infantry\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}," +
        "\"thrall\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1,\"slow\":true}}}";

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
    private const string Radius = "P-MAP-02 = minus=1 min=1";
    private static string Params(string sight = Sight, string radius = Radius)
        => "P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 50\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\n" + sight + "\n" + radius + "\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1";

    private static GameState NewGame(string? parameters = null)
        => new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(parameters ?? Params()));

    private static Brigade West(GameState g) => g.Brigades.Single(b => b.Id == 2);
    private static HexCoord At(int c, int r) => HexCoord.FromOffset(c, r);
    private static void EndTurn(GameState g) => Assert.True(g.ApplyCommand(new EndTurnCommand(g.ActiveFaction)).Accepted);

    private static void RemoveSections(GameState g, Brigade b, int count)
    {
        for (var i = 0; i < count; i++) g.RemoveSectionFromBrigade(b, b.Sections.Count - 1);
    }

    [Fact]
    public void BRG_023_a_brigade_whose_last_section_is_removed_is_disbanded_and_its_hex_is_free()
    {
        var game = NewGame();
        var west = West(game);
        RemoveSections(game, west, 2);
        Assert.Contains(west, game.Brigades); // one Section left
        game.RemoveSectionFromBrigade(west, 0);

        Assert.DoesNotContain(game.Brigades, b => b.Id == 2);
        Assert.Single(game.Brigades);
        // Its hex (west HQ, 0,0) is free: the east Brigade may now be placed there.
        Assert.DoesNotContain(game.Brigades, b => b.Position == At(0, 0));
    }

    [Fact]
    public void BRG_023_a_brigade_with_two_sections_keeps_standing_when_one_is_removed()
    {
        var game = NewGame();
        var west = West(game);
        game.RemoveSectionFromBrigade(west, 0);
        Assert.Equal(2, west.Sections.Count);

        game.RemoveSectionFromBrigade(west, 0);

        Assert.Contains(west, game.Brigades);
    }

    [Fact]
    public void BRG_020_the_minimum_of_three_sections_is_not_enforced_after_forming()
    {
        var game = NewGame();
        var west = West(game);
        game.RemoveSectionFromBrigade(west, 0);

        game.RemoveSectionFromBrigade(west, 0); // 2 -> 1

        Assert.Equal(1, west.Sections.Count);
        Assert.Contains(west, game.Brigades);
    }

    [Fact]
    public void TEC_008_a_new_brigade_after_a_disband_gets_an_id_no_earlier_brigade_had()
    {
        var game = NewGame();
        var west = West(game);
        RemoveSections(game, west, 3); // Brigade 2 is disbanded; 1 remains

        Assert.Equal(3, game.TakeBrigadeId());
        Assert.Equal(4, game.TakeBrigadeId());
        Assert.DoesNotContain(game.Brigades, b => b.Id == 3 || b.Id == 4);
    }

    [Fact]
    public void TEC_008_a_disbanded_highest_id_is_not_reused()
    {
        var game = NewGame();
        RemoveSections(game, game.Brigades.Single(b => b.Id == 2), 3);
        RemoveSections(game, game.Brigades.Single(b => b.Id == 1), 3);

        Assert.Empty(game.Brigades);
        Assert.Equal(3, game.TakeBrigadeId());
    }

    [Fact]
    public void TEC_008_the_id_counter_is_in_the_canonical_text_and_the_hash()
    {
        var a = NewGame();
        var b = NewGame();
        Assert.Contains("nextbrigade=3\n", a.CanonicalText());
        Assert.Equal(a.ComputeHash(), b.ComputeHash());

        b.TakeBrigadeId();

        Assert.Contains("nextbrigade=4\n", b.CanonicalText());
        Assert.NotEqual(a.ComputeHash(), b.ComputeHash());
    }

    [Fact]
    public void TEC_008_replaying_the_same_command_log_and_disband_gives_the_same_hash()
    {
        GameState Run()
        {
            var g = NewGame();
            EndTurn(g);
            RemoveSections(g, g.Brigades.Single(b => b.Id == 2), 3);
            g.TakeBrigadeId();
            EndTurn(g);
            return g;
        }
        var first = Run();
        var registry = CommandRegistry.CreateDefault();
        var run = registry.ReadLog(registry.WriteLog(7, first.Log));
        var replay = GameState.Replay(run.Seed, run.Log, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(Params()));
        replay.RemoveSectionFromBrigade(replay.Brigades.Single(b => b.Id == 2), 0); // a disband is not a command yet, so redo it by hand
        replay.RemoveSectionFromBrigade(replay.Brigades.Single(b => b.Id == 2), 0);
        replay.RemoveSectionFromBrigade(replay.Brigades.Single(b => b.Id == 2), 0);
        replay.TakeBrigadeId();

        Assert.Equal(first.ComputeHash(), Run().ComputeHash());
        Assert.Equal(first.ComputeHash(), replay.ComputeHash());
    }

    [Fact]
    public void BRG_023_a_disbanded_brigade_with_a_pending_capture_mark_completes_no_capture()
    {
        var game = NewGame();
        game.ApplyCommand(new MoveBrigadeCommand("west", 2, new[] { At(0, 1), At(0, 2) }));
        EndTurn(game); // west's capture of supply-1 is pending, started by Brigade 2
        Assert.Equal(2, game.Locations.Single(l => l.Id == "supply-1").CapturingBrigade);
        var west = West(game);
        RemoveSections(game, west, 3);
        // A new Brigade (never id 2) stands on the ST at west's next turn start.
        var fresh = new Brigade(game.TakeBrigadeId(), "west", 40, At(0, 2), new List<Section> { new Section("infantry", 10) });
        ((List<Brigade>)game.Brigades).Add(fresh);

        EndTurn(game);

        Assert.Equal("west", game.ActiveFaction);
        Assert.Null(game.Locations.Single(l => l.Id == "supply-1").Owner);
        Assert.Null(game.Locations.Single(l => l.Id == "supply-1").Capturing);
    }
}
