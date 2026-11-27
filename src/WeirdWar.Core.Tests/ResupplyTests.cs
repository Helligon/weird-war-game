using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class ResupplyTests
{
    // Infantry gives 35 capacity, so a starting 3-Section Brigade holds 105 SP.
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{\"infantry\":{\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    private const string MapJson =
        "{\"version\":1,\"width\":8,\"height\":4,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[7,1],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,2],\"faction\":\"west\"}," +
        "{\"id\":\"supply-1\",\"type\":\"supply\",\"at\":[2,1]}," +
        "{\"id\":\"command-1\",\"type\":\"command\",\"at\":[5,1]}," +
        "{\"id\":\"major-1\",\"type\":\"major\",\"at\":[4,1]}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}}";

    private const string Params = "P-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 5\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2";

    private static GameState NewGame(string parameters = Params)
        => new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(parameters));

    private static FactionState Faction(GameState g, string id) => g.Factions.Single(f => f.Id == id);
    private static LocationState Location(GameState g, string id) => g.Locations.Single(l => l.Id == id);
    private static Brigade BrigadeOf(GameState g, string f) => g.Brigades.Single(b => b.Faction == f);
    private static string Other(string f) => f == "east" ? "west" : "east";
    private static HexCoord At(int col, int row) => HexCoord.FromOffset(col, row);

    /// <summary>A game where the active Faction's Brigade has room (20 SP of 105) and a reserve of 30.</summary>
    private static (GameState game, string faction, Brigade brigade) Setup(string parameters = Params)
    {
        var game = NewGame(parameters);
        var f = game.ActiveFaction!;
        var b = BrigadeOf(game, f);
        b.SP = 20;
        Faction(game, f).Reserve = 30;
        return (game, f, b);
    }

    private static void AssertRejected(GameState game, DistributeSpCommand command)
    {
        var before = game.ComputeHash();
        var logged = game.Log.Count;
        Assert.False(game.ApplyCommand(command).Accepted);
        Assert.Equal(before, game.ComputeHash());
        Assert.Equal(logged, game.Log.Count);
    }

    [Fact]
    public void RES_003_distribute_moves_sp_from_the_reserve_to_the_brigade()
    {
        var (game, f, b) = Setup();

        var result = game.ApplyCommand(new DistributeSpCommand(f, b.Id, 12));

        Assert.True(result.Accepted, result.Reason);
        Assert.Equal(18, Faction(game, f).Reserve);
        Assert.Equal(32, b.SP);
    }

    [Fact]
    public void RES_003_the_whole_reserve_may_be_distributed()
    {
        var (game, f, b) = Setup();

        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 30)).Accepted);
        Assert.Equal(0, Faction(game, f).Reserve);
        Assert.Equal(50, b.SP);
    }

    [Fact]
    public void SUP_028_a_request_above_the_reserve_moves_the_whole_reserve()
    {
        var (game, f, b) = Setup();

        var result = game.ApplyCommand(new DistributeSpCommand(f, b.Id, 31));

        Assert.True(result.Accepted, result.Reason);
        Assert.Equal(0, Faction(game, f).Reserve);
        Assert.Equal(50, b.SP);
    }

    [Fact]
    public void SUP_028_a_request_above_the_room_moves_the_room()
    {
        var (game, f, b) = Setup();
        b.SP = 100; // room 5

        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 20)).Accepted);
        Assert.Equal(105, b.SP);
        Assert.Equal(25, Faction(game, f).Reserve);
    }

    [Fact]
    public void SUP_028_a_request_that_fits_both_moves_exactly_the_request()
    {
        var (game, f, b) = Setup();

        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 7)).Accepted);
        Assert.Equal(27, b.SP);
        Assert.Equal(23, Faction(game, f).Reserve);
    }

    [Fact]
    public void SUP_028_a_request_above_both_the_room_and_the_reserve_moves_the_smaller()
    {
        var (game, f, b) = Setup();
        b.SP = 100; // room 5
        Faction(game, f).Reserve = 3;

        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 50)).Accepted);
        Assert.Equal(103, b.SP);
        Assert.Equal(0, Faction(game, f).Reserve);
    }

    [Fact]
    public void SUP_028_an_empty_reserve_is_rejected_and_changes_nothing()
    {
        var (game, f, b) = Setup();
        Faction(game, f).Reserve = 0;

        AssertRejected(game, new DistributeSpCommand(f, b.Id, 5));
        Assert.Equal(20, b.SP);
    }

    [Fact]
    public void SUP_028_an_amount_above_zero_is_still_rejected_away_from_every_resupply_point()
    {
        var (game, f, b) = Setup();
        b.Position = At(6, 3);
        Assert.True(b.Position.DistanceTo(Location(game, "hq-" + f).Position) >= 2);

        AssertRejected(game, new DistributeSpCommand(f, b.Id, 5));
    }

    [Fact]
    public void MAP_015_a_brigade_on_or_next_to_an_owned_major_st_may_be_resupplied()
    {
        var major = At(4, 1);
        var hexes = new List<HexCoord> { major };
        hexes.AddRange(major.Neighbours());
        foreach (var hex in hexes)
        {
            var (game, f, b) = Setup();
            Location(game, "major-1").Owner = f;
            b.Position = hex;
            Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 5)).Accepted, hex.ToString());
        }
    }

    [Fact]
    public void SUP_013_a_neutral_or_enemy_major_st_cannot_resupply()
    {
        var (game, f, b) = Setup();
        b.Position = At(4, 1);
        AssertRejected(game, new DistributeSpCommand(f, b.Id, 5)); // neutral
        Location(game, "major-1").Owner = Other(f);
        AssertRejected(game, new DistributeSpCommand(f, b.Id, 5));
    }

    [Fact]
    public void SUP_027_an_enemy_brigade_on_the_own_hq_blocks_resupply()
    {
        var (game, f, b) = Setup();
        BrigadeOf(game, Other(f)).Position = Location(game, "hq-" + f).Position;

        AssertRejected(game, new DistributeSpCommand(f, b.Id, 5));
    }

    [Theory]
    [InlineData("supply-1")]
    [InlineData("major-1")]
    public void SUP_027_an_enemy_brigade_on_an_owned_supply_or_major_st_blocks_resupply(string id)
    {
        var (game, f, b) = Setup();
        var st = Location(game, id);
        st.Owner = f;
        b.Position = st.Position;
        BrigadeOf(game, Other(f)).Position = st.Position;

        AssertRejected(game, new DistributeSpCommand(f, b.Id, 5));
    }

    [Fact]
    public void SUP_027_a_brigade_in_range_of_another_unblocked_resupply_point_is_still_resupplied()
    {
        var (game, f, b) = Setup();
        Location(game, "supply-1").Owner = f;
        Location(game, "major-1").Owner = f;
        b.Position = At(3, 1); // next to both supply-1 (2,1) and major-1 (4,1)
        BrigadeOf(game, Other(f)).Position = At(2, 1); // blocks supply-1 only

        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 5)).Accepted);
    }

    [Fact]
    public void SUP_027_an_enemy_brigade_next_to_a_resupply_point_does_not_block()
    {
        var (game, f, b) = Setup();
        BrigadeOf(game, Other(f)).Position = Location(game, "hq-" + f).Position.Neighbours().First();

        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 5)).Accepted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RES_003_an_amount_below_one_is_rejected(int amount)
    {
        var (game, f, b) = Setup();

        AssertRejected(game, new DistributeSpCommand(f, b.Id, amount));
        Assert.Equal(30, Faction(game, f).Reserve);
        Assert.Equal(20, b.SP);
    }

    [Fact]
    public void RES_003_only_the_owner_may_distribute_to_its_brigade()
    {
        var (game, f, _) = Setup();
        var enemy = Other(f);
        BrigadeOf(game, enemy).SP = 10;
        Faction(game, enemy).Reserve = 30;
        var enemyBrigade = BrigadeOf(game, enemy);

        // The active Faction names the enemy Brigade.
        AssertRejected(game, new DistributeSpCommand(f, enemyBrigade.Id, 5));
        // The inactive Faction acts out of turn (TRN-006).
        AssertRejected(game, new DistributeSpCommand(enemy, enemyBrigade.Id, 5));
        // No such Brigade.
        AssertRejected(game, new DistributeSpCommand(f, 99, 5));
        Assert.Equal(10, enemyBrigade.SP);
        Assert.Equal(30, Faction(game, enemy).Reserve);
        Assert.Equal(30, Faction(game, f).Reserve);
    }

    [Fact]
    public void TRN_005_distribute_is_an_actions_phase_command()
    {
        var (game, f, b) = Setup();
        game.Phase = TurnPhase.Income;

        var before = game.ComputeHash();
        var result = game.ApplyCommand(new DistributeSpCommand(f, b.Id, 5));

        Assert.False(result.Accepted);
        Assert.Contains("Actions phase", result.Reason);
        Assert.Equal(before, game.ComputeHash());
    }

    [Fact]
    public void SUP_017_a_brigade_on_its_own_hq_hex_may_be_resupplied()
    {
        var (game, f, b) = Setup();
        Assert.Equal(Location(game, "hq-" + f).Position, b.Position); // the starting Brigade stands on the HQ

        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 5)).Accepted);
    }

    [Fact]
    public void P_SUP_08_a_brigade_on_a_hex_adjacent_to_its_hq_may_be_resupplied()
    {
        var hq = Location(NewGame(), "hq-" + NewGame().ActiveFaction).Position;
        foreach (var neighbour in hq.Neighbours())
        {
            var (game, f, b) = Setup();
            b.Position = neighbour;
            Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 5)).Accepted, neighbour.ToString());
        }
    }

    [Fact]
    public void SUP_017_a_brigade_on_or_next_to_an_owned_supply_st_may_be_resupplied()
    {
        var supply = At(2, 1);
        var hexes = new List<HexCoord> { supply };
        hexes.AddRange(supply.Neighbours());
        foreach (var hex in hexes)
        {
            var (game, f, b) = Setup();
            Location(game, "supply-1").Owner = f;
            b.Position = hex;
            Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 5)).Accepted, hex.ToString());
        }
    }

    [Fact]
    public void SUP_012_two_or_more_hexes_from_every_owned_supply_st_and_hq_is_rejected()
    {
        var (game, f, b) = Setup();
        Location(game, "supply-1").Owner = f;
        Location(game, "major-1").Owner = f;
        b.Position = At(4, 3);
        foreach (var point in game.Locations.Where(l => l.Type is LocationType.Hq or LocationType.Supply or LocationType.Major))
            Assert.True(b.Position.DistanceTo(point.Position) >= 2, point.Id);

        var before = game.ComputeHash();
        var result = game.ApplyCommand(new DistributeSpCommand(f, b.Id, 5));

        Assert.False(result.Accepted);
        Assert.Equal(before, game.ComputeHash());
        Assert.Equal(20, b.SP);
        Assert.Equal(30, Faction(game, f).Reserve);
    }

    [Fact]
    public void SUP_012_exactly_two_hexes_from_the_supply_st_is_rejected()
    {
        var (game, f, b) = Setup();
        Location(game, "supply-1").Owner = f;
        var hq = Location(game, "hq-" + f).Position;
        b.Position = At(2, 1).Neighbours().SelectMany(n => n.Neighbours())
            .First(h => h.DistanceTo(At(2, 1)) == 2 && h.DistanceTo(hq) >= 2);

        AssertRejected(game, new DistributeSpCommand(f, b.Id, 5));
    }

    [Fact]
    public void SUP_013_a_neutral_supply_st_cannot_resupply()
    {
        var (game, f, b) = Setup();
        b.Position = At(3, 1); // next to the neutral supply-1, far from both HQs
        Assert.Null(Location(game, "supply-1").Owner);
        Assert.Equal(1, b.Position.DistanceTo(At(2, 1)));

        var before = game.ComputeHash();
        var result = game.ApplyCommand(new DistributeSpCommand(f, b.Id, 5));

        Assert.False(result.Accepted);
        Assert.Equal(before, game.ComputeHash());
        Assert.Equal(20, b.SP);
    }

    [Fact]
    public void SUP_013_an_enemy_owned_supply_st_and_the_enemy_hq_cannot_resupply()
    {
        var (game, f, b) = Setup();
        Location(game, "supply-1").Owner = Other(f);
        b.Position = At(3, 1);
        AssertRejected(game, new DistributeSpCommand(f, b.Id, 5));

        b.Position = Location(game, "hq-" + Other(f)).Position;
        AssertRejected(game, new DistributeSpCommand(f, b.Id, 5));
    }

    [Fact]
    public void SUP_012_only_supply_type_sts_resupply_not_command_sts()
    {
        var (game, f, b) = Setup();
        Location(game, "command-1").Owner = f;
        b.Position = Location(game, "command-1").Position;

        AssertRejected(game, new DistributeSpCommand(f, b.Id, 5));
    }

    [Fact]
    public void P_SUP_08_the_range_comes_from_the_parameter()
    {
        var (game, f, b) = Setup(Params.Replace("P-SUP-08 = 1", "P-SUP-08 = 2"));
        Location(game, "supply-1").Owner = f;
        b.Position = At(4, 1); // exactly 2 from supply-1
        Assert.Equal(2, b.Position.DistanceTo(At(2, 1)));

        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 5)).Accepted);
    }

    [Fact]
    public void SUP_018_distribution_costs_nothing_extra_and_has_no_action_limit()
    {
        var (game, f, b) = Setup();
        var enemyReserve = Faction(game, Other(f)).Reserve;
        var enemySp = BrigadeOf(game, Other(f)).SP;
        var cp = Faction(game, f).Cp;
        var position = b.Position;
        var turn = game.TurnNumber;

        for (var i = 0; i < 6; i++) Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 1)).Accepted);

        Assert.Equal(24, Faction(game, f).Reserve);
        Assert.Equal(26, b.SP);
        Assert.Equal(cp, Faction(game, f).Cp);
        Assert.Equal(position, b.Position);
        Assert.Equal(turn, game.TurnNumber);
        Assert.Equal(f, game.ActiveFaction);
        Assert.Equal(TurnPhase.Actions, game.Phase);
        Assert.Equal(enemyReserve, Faction(game, Other(f)).Reserve);
        Assert.Equal(enemySp, BrigadeOf(game, Other(f)).SP);
    }

    [Fact]
    public void SUP_021_resupply_never_takes_a_brigade_above_its_capacity()
    {
        var (game, f, b) = Setup();
        b.SP = 100; // capacity 105, room 5

        var result = game.ApplyCommand(new DistributeSpCommand(f, b.Id, 20));

        Assert.True(result.Accepted, result.Reason);
        Assert.Equal(105, b.SP);
        Assert.Equal(25, Faction(game, f).Reserve); // only the 5 that moved leave the reserve
    }

    [Fact]
    public void SUP_021_a_brigade_at_its_capacity_receives_nothing_more()
    {
        var (game, f, b) = Setup();
        b.SP = 105;
        var before = game.ComputeHash();

        AssertRejected(game, new DistributeSpCommand(f, b.Id, 1));
        Assert.Equal(105, b.SP);
        Assert.Equal(30, Faction(game, f).Reserve);
        Assert.Equal(before, game.ComputeHash());
    }

    [Fact]
    public void BRG_019_a_brigade_above_its_lowered_maximum_keeps_its_sp()
    {
        var (game, _, b) = Setup();
        b.SP = 105;
        LowerCapacity(b); // 2 Sections: maximum 70, SP 105 stays

        Assert.Equal(70, game.Supply!.CapacityOf(b));
        Assert.Equal(105, b.SP);
    }

    [Fact]
    public void BRG_019_a_brigade_above_its_new_maximum_receives_nothing_more()
    {
        var (game, f, b) = Setup();
        b.SP = 105;
        LowerCapacity(b);

        AssertRejected(game, new DistributeSpCommand(f, b.Id, 5));
        Assert.Equal(105, b.SP);
        Assert.Equal(30, Faction(game, f).Reserve);
    }

    [Fact]
    public void BRG_019_once_below_the_new_maximum_it_is_resupplied_only_up_to_it()
    {
        var (game, f, b) = Setup();
        b.SP = 105;
        LowerCapacity(b);
        b.SP = 60; // spent down below the new maximum of 70 (not the old 105)

        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 30)).Accepted);

        Assert.Equal(70, b.SP);
        Assert.Equal(20, Faction(game, f).Reserve);
    }

    [Fact]
    public void TEC_008_distribute_round_trips_and_replay_reproduces_the_hash_including_a_clamped_distribution()
    {
        // Both Brigades start at 100 of 105 SP, so a request of 8 (within the reserve) is clamped to the room of 5.
        var parameters = Params.Replace("P-BRG-09 = 90", "P-BRG-09 = 100");
        var registry = CommandRegistry.CreateDefault();
        var game = new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(parameters));
        var f = game.ActiveFaction!;
        var b = BrigadeOf(game, f);
        var reserve = Faction(game, f).Reserve;
        Assert.True(reserve >= 8, "fixture: the request must fit the reserve");

        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 8)).Accepted);
        Assert.Equal(105, b.SP);
        Assert.Equal(reserve - 5, Faction(game, f).Reserve); // only the 5 that moved leave the reserve
        AssertRejected(game, new DistributeSpCommand(f, b.Id, 1)); // now at capacity

        var command = new DistributeSpCommand(f, b.Id, 8);
        Assert.Equal(command, registry.ReadCommand(registry.WriteCommand(command)));
        Assert.Equal("{\"amount\":8,\"brigade\":" + b.Id + ",\"faction\":\"" + f + "\",\"type\":\"distributeSp\"}", registry.WriteCommand(command));

        var run = registry.ReadLog(registry.WriteLog(7, game.Log));
        var replayed = GameState.Replay(run.Seed, run.Log, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(parameters));
        Assert.Equal(game.ComputeHash(), replayed.ComputeHash());
        Assert.Equal(105, BrigadeOf(replayed, f).SP);
    }

    /// <summary>Test fixture: Section transfers do not exist yet (a later Feature), so drop the last Section to lower the maximum (BRG-019).</summary>
    private static void LowerCapacity(Brigade brigade) => brigade.RemoveSection(brigade.Sections.Count - 1);
}
