using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class TurnTests
{
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{\"infantry\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    private const string MapJson =
        "{\"version\":1,\"width\":6,\"height\":4,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[5,1],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,2],\"faction\":\"west\"}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}}";

    private static GameState NewGame(ulong seed = 7)
        => new GameState(seed, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse("P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 5\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1"));

    private static string Other(string f) => f == "east" ? "west" : "east";

    [Fact]
    public void TRN_006_a_command_from_the_inactive_faction_is_rejected_and_changes_nothing()
    {
        var game = NewGame();
        var hash = game.ComputeHash();

        var result = game.ApplyCommand(new AddToTallyCommand(1, Other(game.ActiveFaction!)));

        Assert.False(result.Accepted);
        Assert.Contains("active Faction", result.Reason);
        Assert.Equal(hash, game.ComputeHash());
        Assert.Equal(0, game.Log.Count);
    }

    [Fact]
    public void TRN_006_a_command_with_no_faction_or_an_unknown_faction_is_rejected_in_a_campaign()
    {
        var game = NewGame();

        Assert.False(game.ApplyCommand(new AddToTallyCommand(1)).Accepted);
        Assert.False(game.ApplyCommand(new AddToTallyCommand(1, "north")).Accepted);
        Assert.False(game.ApplyCommand(new EndTurnCommand("")).Accepted);
    }

    [Fact]
    public void TRN_006_the_inactive_faction_cannot_end_the_turn()
    {
        var game = NewGame();

        Assert.False(game.ApplyCommand(new EndTurnCommand(Other(game.ActiveFaction!))).Accepted);
        Assert.Equal(1, game.TurnNumber);
    }

    [Fact]
    public void TRN_006_a_state_without_a_campaign_has_no_turns_and_accepts_unnamed_commands()
    {
        var state = new GameState(1);

        Assert.Null(state.ActiveFaction);
        Assert.True(state.ApplyCommand(new AddToTallyCommand(1)).Accepted);
        Assert.False(state.ApplyCommand(new EndTurnCommand("")).Accepted);
    }

    [Fact]
    public void TRN_007_turns_alternate_and_each_faction_gets_a_full_turn()
    {
        var game = NewGame();
        var first = game.ActiveFaction!;
        var second = Other(first);

        Assert.True(game.ApplyCommand(new EndTurnCommand(first)).Accepted);
        Assert.Equal(second, game.ActiveFaction);
        Assert.True(game.ApplyCommand(new AddToTallyCommand(1, second)).Accepted);
        Assert.False(game.ApplyCommand(new AddToTallyCommand(1, first)).Accepted);

        Assert.True(game.ApplyCommand(new EndTurnCommand(second)).Accepted);
        Assert.Equal(first, game.ActiveFaction);
    }

    [Fact]
    public void TRN_035_each_faction_turn_adds_one_to_the_turn_number()
    {
        var game = NewGame();
        Assert.Equal(1, game.TurnNumber); // the first player's turn is turn 1

        Assert.True(game.ApplyCommand(new EndTurnCommand(game.ActiveFaction!)).Accepted);
        Assert.Equal(2, game.TurnNumber); // the second player's turn is turn 2, not a second part of round 1

        Assert.True(game.ApplyCommand(new EndTurnCommand(game.ActiveFaction!)).Accepted);
        Assert.Equal(3, game.TurnNumber);
    }

    [Fact]
    public void TRN_033_the_same_seed_gives_the_same_first_player()
    {
        for (ulong seed = 1; seed <= 20; seed++)
            Assert.Equal(NewGame(seed).ActiveFaction, NewGame(seed).ActiveFaction);
    }

    [Fact]
    public void TRN_033_the_first_player_is_drawn_from_the_seeded_source()
    {
        var game = NewGame(7);

        Assert.Equal("west", game.ActiveFaction); // pinned: SplitMix64 seed 7, first draw, mod 2 (computed in Python)
        Assert.Equal(11400714819323198492UL, game.Random.Position);
    }

    [Fact]
    public void TRN_033_both_factions_come_up_as_first_player_across_seeds()
    {
        var firsts = Enumerable.Range(1, 40).Select(s => NewGame((ulong)s).ActiveFaction).Distinct().ToList();

        Assert.Contains("east", firsts);
        Assert.Contains("west", firsts);
    }

    [Fact]
    public void TRN_005_phases_run_in_order_on_each_turn()
    {
        var game = NewGame();
        var first = game.ActiveFaction!;
        var second = Other(first);
        var seen = new List<(TurnPhase, string)>();
        game.PhaseObserver = (p, f) => seen.Add((p, f));

        game.ApplyCommand(new EndTurnCommand(first));

        Assert.Equal(new[]
        {
            (TurnPhase.EndOfTurn, first), (TurnPhase.StartOfTurn, second), (TurnPhase.Income, second), (TurnPhase.Draw, second), (TurnPhase.Actions, second),
        }, seen);
        Assert.Equal(TurnPhase.Actions, game.Phase);
    }

    [Fact]
    public void TRN_034_the_first_player_gets_income_on_turn_1_and_the_game_rests_in_actions()
    {
        var seen = new List<(TurnPhase, string)>();
        var game = new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse("P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 5\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1"),
            (p, f) => seen.Add((p, f)));
        var first = game.ActiveFaction!;

        Assert.Equal(new[] { (TurnPhase.StartOfTurn, first), (TurnPhase.Income, first), (TurnPhase.Draw, first), (TurnPhase.Actions, first) }, seen);
        Assert.Equal(TurnPhase.Actions, game.Phase);
        Assert.Equal(1, game.TurnNumber);
        Assert.Equal(2, game.Factions.Single(f => f.Id == first).Cp); // P-RES-02 paid on turn 1
    }

    [Fact]
    public void TRN_005_an_action_command_is_rejected_before_the_actions_phase_and_accepted_in_it()
    {
        var game = NewGame();
        var f = game.ActiveFaction!;
        foreach (var early in new[] { TurnPhase.StartOfTurn, TurnPhase.Income, TurnPhase.EndOfTurn })
        {
            game.Phase = early;
            var result = game.ApplyCommand(new AddToTallyCommand(1, f));
            Assert.False(result.Accepted);
            Assert.Contains("Actions phase", result.Reason);
            Assert.False(game.ApplyCommand(new EndTurnCommand(f)).Accepted);
        }
        Assert.Equal(0, game.Tally);

        game.Phase = TurnPhase.Actions;
        Assert.True(game.ApplyCommand(new AddToTallyCommand(1, f)).Accepted);
    }

    [Fact]
    public void TRN_002_only_the_core_framework_commands_exist_in_cc1()
    {
        // No form-Brigade command exists yet; the play-Card (CD1-141), move (CC1-72), distribute (CC1-74) transfer (FR1-115) and upgradeFob (FR1-116) commands are here.
        var names = typeof(ICommand).Assembly.GetTypes()
            .Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(t => t.Name).OrderBy(n => n).ToArray();

        Assert.Equal(new[] { "AddToTallyCommand", "DistributeSpCommand", "EndTurnCommand", "MoveBrigadeCommand", "PlayCardCommand", "RollIntoTallyCommand", "TransferSectionCommand", "UpgradeFobCommand" }, names);
    }

    [Fact]
    public void TEC_008_end_turn_round_trips_and_replay_reproduces_the_hash()
    {
        var registry = CommandRegistry.CreateDefault();
        var game = NewGame();
        var first = game.ActiveFaction!;
        game.ApplyCommand(new AddToTallyCommand(2, first));
        game.ApplyCommand(new EndTurnCommand(first));
        game.ApplyCommand(new RollIntoTallyCommand(6, Other(first)));
        game.ApplyCommand(new EndTurnCommand(Other(first)));

        var run = registry.ReadLog(registry.WriteLog(7, game.Log));
        var replayed = GameState.Replay(run.Seed, run.Log, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse("P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 5\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1"));

        Assert.Equal(game.ComputeHash(), replayed.ComputeHash());
        Assert.Equal(first, replayed.ActiveFaction);
        Assert.Equal(3, replayed.TurnNumber);
    }
}
