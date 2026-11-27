using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class TransferSectionTests
{
    // 12 x 8 map as in DisbandBrigadeTests. West HQ (0,0), supply-1 (0,2). Seed 7 gives west the first turn.
    // Brigade 1 is east, brigade 2 is west. Extra Brigades are placed by the test (no forming command exists yet).
    // Infantry gives 35 capacity; "heavy" gives 50, so moving it out of a Brigade drops that Brigade's capacity (BRG-019).
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{" +
        "\"infantry\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}," +
        "\"heavy\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":50,\"consumptionPerHex\":1}}}";

    private const string MapJson =
        "{\"version\":1,\"width\":12,\"height\":8,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[11,7],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,0],\"faction\":\"west\"}," +
        "{\"id\":\"supply-1\",\"type\":\"supply\",\"at\":[0,2]}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}}";

    private static string Params(int maxSections = 5)
        => "P-BRG-01 = " + maxSections + "\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 50\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\n" +
           "P-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1";

    private static GameState NewGame(int maxSections = 5)
        => new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(Params(maxSections)));

    private static HexCoord At(int c, int r) => HexCoord.FromOffset(c, r);

    /// <summary>Places a Brigade. Sections are "type" or "type:health".</summary>
    private static Brigade Add(GameState g, string faction, int sp, HexCoord at, params string[] sections)
    {
        var list = sections.Select(s => { var p = s.Split(':'); return new Section(p[0], p.Length > 1 ? int.Parse(p[1]) : 10); }).ToList();
        var b = new Brigade(g.TakeBrigadeId(), faction, sp, at, list);
        g.AddBrigade(b);
        return b;
    }

    private static string[] Inf(int n) => Enumerable.Repeat("infantry", n).ToArray();

    private static void AssertRejected(GameState game, ICommand command)
    {
        var before = game.ComputeHash();
        var logged = game.Log.Count;
        Assert.False(game.ApplyCommand(command).Accepted);
        Assert.Equal(before, game.ComputeHash());
        Assert.Equal(logged, game.Log.Count);
    }

    [Fact]
    public void BRG_016_a_section_moves_between_adjacent_friendly_brigades_and_keeps_its_type_and_health()
    {
        var game = NewGame();
        var source = Add(game, "west", 40, At(3, 3), "infantry:10", "heavy:7");
        var target = Add(game, "west", 10, At(4, 3), "infantry", "infantry", "infantry");

        var result = game.ApplyCommand(new TransferSectionCommand("west", source.Id, 1, target.Id));

        Assert.True(result.Accepted);
        Assert.Equal(4, target.Sections.Count);
        Assert.Single(source.Sections);
        Assert.Equal("heavy", target.Sections[3].TypeId);
        Assert.Equal(7, target.Sections[3].Health);
        Assert.Equal("infantry", source.Sections[0].TypeId);
    }

    [Fact]
    public void BRG_017_brigades_that_are_not_adjacent_are_rejected_with_a_reason_and_the_hash_is_unchanged()
    {
        var game = NewGame();
        var source = Add(game, "west", 40, At(3, 3), Inf(2));
        var target = Add(game, "west", 10, At(6, 3), Inf(3));
        var command = new TransferSectionCommand("west", source.Id, 0, target.Id);

        Assert.False(string.IsNullOrEmpty(command.Validate(game)));
        AssertRejected(game, command);
    }

    [Fact]
    public void BRG_017_a_transfer_costs_no_sp_and_is_accepted_at_sp_0()
    {
        var game = NewGame();
        var source = Add(game, "west", 0, At(3, 3), Inf(2));
        var target = Add(game, "west", 0, At(4, 3), Inf(3));
        var reserve = game.Factions.Single(f => f.Id == "west").Reserve;

        Assert.True(game.ApplyCommand(new TransferSectionCommand("west", source.Id, 0, target.Id)).Accepted);

        Assert.Equal(reserve, game.Factions.Single(f => f.Id == "west").Reserve);
        Assert.Equal(0, source.SP);
        Assert.Equal(0, target.SP);
    }

    [Fact]
    public void BRG_018_the_moved_section_takes_no_sp_with_it()
    {
        var game = NewGame();
        var source = Add(game, "west", 40, At(3, 3), Inf(2));
        var target = Add(game, "west", 10, At(4, 3), Inf(3));

        Assert.True(game.ApplyCommand(new TransferSectionCommand("west", source.Id, 0, target.Id)).Accepted);

        Assert.Equal(40, source.SP);
        Assert.Equal(10, target.SP);
    }

    [Fact]
    public void BRG_019_a_source_whose_capacity_drops_below_its_sp_keeps_all_its_sp()
    {
        var game = NewGame();
        var source = Add(game, "west", 100, At(3, 3), "heavy", "infantry", "infantry"); // capacity 120
        var target = Add(game, "west", 105, At(4, 3), Inf(3)); // capacity 105: full
        Assert.Equal(120, game.Supply!.CapacityOf(source));

        Assert.True(game.ApplyCommand(new TransferSectionCommand("west", source.Id, 0, target.Id)).Accepted);

        Assert.Equal(70, game.Supply.CapacityOf(source));
        Assert.Equal(100, source.SP);
        Assert.Equal(105, target.SP);
    }

    [Fact]
    public void BRG_019_an_overflowed_brigade_is_not_resupplied_above_its_new_maximum()
    {
        var game = NewGame();
        var source = Add(game, "west", 100, At(0, 1), "heavy", "infantry", "infantry"); // next to the west HQ
        var target = Add(game, "west", 0, At(1, 1), Inf(3));
        Assert.Equal(1, source.Position.DistanceTo(target.Position));
        Assert.True(game.ApplyCommand(new TransferSectionCommand("west", source.Id, 0, target.Id)).Accepted);
        Assert.Equal(70, game.Supply!.CapacityOf(source));

        AssertRejected(game, new DistributeSpCommand("west", source.Id, 10)); // 100 SP, maximum 70: no room

        source.SP = 60; // SP has fallen below the new maximum
        Assert.True(game.ApplyCommand(new DistributeSpCommand("west", source.Id, 30)).Accepted);
        Assert.Equal(70, source.SP);
    }

    [Fact]
    public void BRG_020_a_source_with_three_sections_may_give_one_up()
    {
        var game = NewGame();
        var source = Add(game, "west", 40, At(3, 3), Inf(3));
        var target = Add(game, "west", 10, At(4, 3), Inf(3));

        Assert.True(game.ApplyCommand(new TransferSectionCommand("west", source.Id, 0, target.Id)).Accepted);

        Assert.Equal(2, source.Sections.Count);
    }

    [Fact]
    public void BRG_021_a_target_with_five_sections_is_rejected_and_nothing_changes()
    {
        var game = NewGame();
        var source = Add(game, "west", 40, At(3, 3), Inf(2));
        var target = Add(game, "west", 10, At(4, 3), Inf(5));

        AssertRejected(game, new TransferSectionCommand("west", source.Id, 0, target.Id));

        Assert.Equal(2, source.Sections.Count);
        Assert.Equal(5, target.Sections.Count);
    }

    [Fact]
    public void BRG_021_the_maximum_comes_from_p_brg_01()
    {
        var game = NewGame(maxSections: 4);
        var source = Add(game, "west", 40, At(3, 3), Inf(2));
        var target = Add(game, "west", 10, At(4, 3), Inf(4));

        AssertRejected(game, new TransferSectionCommand("west", source.Id, 0, target.Id));

        Assert.Throws<ParameterFormatException>(() => NewGame(maxSections: 0));
    }

    [Fact]
    public void BRG_023_a_source_whose_last_section_moves_is_disbanded()
    {
        var game = NewGame();
        var source = Add(game, "west", 40, At(3, 3), "heavy:6");
        var target = Add(game, "west", 10, At(4, 3), Inf(3));

        Assert.True(game.ApplyCommand(new TransferSectionCommand("west", source.Id, 0, target.Id)).Accepted);

        Assert.DoesNotContain(game.Brigades, b => b.Id == source.Id);
        Assert.Equal(4, target.Sections.Count);
        Assert.Equal(6, target.Sections[3].Health);
    }

    [Fact]
    public void DLV_013_a_brigade_of_another_faction_is_rejected()
    {
        var game = NewGame();
        var source = Add(game, "west", 40, At(3, 3), Inf(2));
        var friend = Add(game, "west", 10, At(4, 3), Inf(3));

        AssertRejected(game, new TransferSectionCommand("west", source.Id, 0, 1)); // Brigade 1 is east's
        AssertRejected(game, new TransferSectionCommand("west", 1, 0, friend.Id)); // the source is east's
    }

    [Fact]
    public void DLV_013_a_bad_section_index_the_same_brigade_or_an_unknown_brigade_is_rejected()
    {
        var game = NewGame();
        var source = Add(game, "west", 40, At(3, 3), Inf(2));
        var target = Add(game, "west", 10, At(4, 3), Inf(3));

        AssertRejected(game, new TransferSectionCommand("west", source.Id, 2, target.Id));
        AssertRejected(game, new TransferSectionCommand("west", source.Id, -1, target.Id));
        AssertRejected(game, new TransferSectionCommand("west", source.Id, 0, source.Id));
        AssertRejected(game, new TransferSectionCommand("west", source.Id, 0, 99));
        AssertRejected(game, new TransferSectionCommand("west", 99, 0, target.Id));
    }

    [Fact]
    public void DLV_013_a_transfer_out_of_turn_is_rejected()
    {
        var game = NewGame(); // west is active
        var east1 = new Brigade(game.TakeBrigadeId(), "east", 40, At(8, 4), new List<Section> { new Section("infantry", 10), new Section("infantry", 10) });
        var east2 = new Brigade(game.TakeBrigadeId(), "east", 40, At(9, 4), new List<Section> { new Section("infantry", 10) });
        game.AddBrigade(east1);
        game.AddBrigade(east2);

        AssertRejected(game, new TransferSectionCommand("east", east1.Id, 0, east2.Id));
    }

    [Fact]
    public void DLV_013_a_transfer_outside_the_actions_phase_is_rejected()
    {
        var game = NewGame();
        var source = Add(game, "west", 40, At(3, 3), Inf(2));
        var target = Add(game, "west", 10, At(4, 3), Inf(3));
        game.Phase = TurnPhase.Income;

        AssertRejected(game, new TransferSectionCommand("west", source.Id, 0, target.Id));
    }

    [Fact]
    public void TEC_008_the_command_round_trips_through_json_and_replay_gives_the_same_hash()
    {
        GameState Build(out Brigade source, out Brigade target)
        {
            var g = NewGame();
            source = Add(g, "west", 40, At(3, 3), "infantry:10", "heavy:7");
            target = Add(g, "west", 10, At(4, 3), Inf(3));
            return g;
        }
        var game = Build(out var s, out var t);
        var command = new TransferSectionCommand("west", s.Id, 1, t.Id);
        var registry = CommandRegistry.CreateDefault();
        Assert.Equal("{\"faction\":\"west\",\"index\":1,\"source\":" + s.Id + ",\"target\":" + t.Id + ",\"type\":\"transferSection\"}", registry.WriteCommand(command));
        Assert.Equal(command, registry.ReadCommand(registry.WriteCommand(command)));
        Assert.True(game.ApplyCommand(command).Accepted);

        var run = registry.ReadLog(registry.WriteLog(7, game.Log));
        var replay = Build(out _, out _);
        foreach (var entry in run.Log.Entries) Assert.True(replay.ApplyCommand(entry.Command).Accepted);

        Assert.Equal(game.ComputeHash(), replay.ComputeHash());
        Assert.Contains("sections=infantry:10,infantry:10,infantry:10,heavy:7 ", game.CanonicalText());
        Assert.Contains("sections=infantry:10 ", game.CanonicalText());
    }
}
