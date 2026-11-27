using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class FobTests
{
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{\"infantry\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    private const string MapJson =
        "{\"version\":1,\"width\":10,\"height\":4,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[9,1],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,2],\"faction\":\"west\"}," +
        "{\"id\":\"supply-1\",\"type\":\"supply\",\"at\":[2,1]}," +
        "{\"id\":\"supply-2\",\"type\":\"supply\",\"at\":[5,1]}," +
        "{\"id\":\"supply-3\",\"type\":\"supply\",\"at\":[7,1]}," +
        "{\"id\":\"supply-4\",\"type\":\"supply\",\"at\":[0,0]}," +
        "{\"id\":\"command-1\",\"type\":\"command\",\"at\":[3,3]}," +
        "{\"id\":\"major-1\",\"type\":\"major\",\"at\":[7,3]}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}}";

    // Base SP 5, Supply ST bonus 3, FOB income 7, FOB sight 4 (radius 3), upgrade 5 SP and CP 2, 5, 8, ...: each amount has its own value.
    private const string Params = "P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 5\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\nP-FOW-03 = hq=3 fob=4 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=2 cpStep=3\nP-MAP-14 = 7";

    private static GameState NewGame(string parameters = Params)
        => new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(parameters));

    private static FactionState Faction(GameState g, string id) => g.Factions.Single(f => f.Id == id);
    private static LocationState Loc(GameState g, string id) => g.Locations.Single(l => l.Id == id);
    private static string Other(string f) => f == "east" ? "west" : "east";
    private static HexCoord At(int c, int r) => HexCoord.FromOffset(c, r);

    /// <summary>The active Faction owns supply-1 to supply-3 and has reserve 50 and CP 20.</summary>
    private static (GameState game, string f) Setup(string parameters = Params)
    {
        var game = NewGame(parameters);
        var f = game.ActiveFaction!;
        foreach (var id in new[] { "supply-1", "supply-2", "supply-3" }) Loc(game, id).Owner = f;
        Faction(game, f).Reserve = 50;
        Faction(game, f).Cp = 20;
        return (game, f);
    }

    private static void Disown(GameState game, params string[] ids)
    {
        foreach (var id in ids) Loc(game, id).Owner = null;
    }

    private static Brigade Add(GameState g, string faction, int sp, HexCoord at)
    {
        var b = new Brigade(g.TakeBrigadeId(), faction, sp, at, new List<Section> { new Section("infantry", 10), new Section("infantry", 10), new Section("infantry", 10) });
        g.AddBrigade(b);
        return b;
    }

    private static void AssertRejected(GameState game, ICommand command, string? reasonPart = null)
    {
        var before = game.ComputeHash();
        var logged = game.Log.Count;
        var result = game.ApplyCommand(command);
        Assert.False(result.Accepted);
        if (reasonPart != null) Assert.Contains(reasonPart, result.Reason);
        Assert.Equal(before, game.ComputeHash());
        Assert.Equal(logged, game.Log.Count);
    }

    private static void EndTurn(GameState g) => Assert.True(g.ApplyCommand(new EndTurnCommand(g.ActiveFaction)).Accepted);

    [Fact]
    public void MAP_012_upgrade_turns_an_owned_supply_st_into_an_fob_at_once_and_takes_the_cost()
    {
        var (game, f) = Setup();

        var result = game.ApplyCommand(new UpgradeFobCommand(f, "supply-1"));

        Assert.True(result.Accepted, result.Reason);
        Assert.Equal(LocationType.Fob, Loc(game, "supply-1").Type);
        Assert.Equal(f, Loc(game, "supply-1").Owner);
        Assert.Equal(50 - 5, Faction(game, f).Reserve);
        Assert.Equal(20 - 2, Faction(game, f).Cp);
        Assert.Equal(1, Faction(game, f).FobUpgrades);
    }

    [Fact]
    public void MAP_012_no_brigade_is_needed_on_the_hex()
    {
        var (game, f) = Setup();
        Assert.DoesNotContain(game.Brigades, b => b.Position.Equals(At(2, 1)));

        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
    }

    [Fact]
    public void MAP_025_each_upgrade_costs_more_cp_than_the_one_before_and_the_same_sp()
    {
        var (game, f) = Setup();

        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
        Assert.Equal(18, Faction(game, f).Cp);
        Assert.Equal(45, Faction(game, f).Reserve);
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-2")).Accepted);
        Assert.Equal(18 - 5, Faction(game, f).Cp);   // cp + cpStep
        Assert.Equal(40, Faction(game, f).Reserve);   // the same standard SP
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-3")).Accepted);
        Assert.Equal(13 - 8, Faction(game, f).Cp);   // cp + 2 * cpStep
    }

    [Fact]
    public void MAP_012_not_enough_sp_is_rejected_with_a_reason_and_nothing_is_spent()
    {
        var (game, f) = Setup();
        Faction(game, f).Reserve = 4;

        AssertRejected(game, new UpgradeFobCommand(f, "supply-1"), "SP");
        Assert.Equal(4, Faction(game, f).Reserve);
        Assert.Equal(20, Faction(game, f).Cp);
        Assert.Equal(LocationType.Supply, Loc(game, "supply-1").Type);
    }

    [Fact]
    public void MAP_012_not_enough_cp_is_rejected_with_a_reason_and_nothing_is_spent()
    {
        var (game, f) = Setup();
        Faction(game, f).Cp = 1;

        AssertRejected(game, new UpgradeFobCommand(f, "supply-1"), "CP");
        Assert.Equal(50, Faction(game, f).Reserve);
        Assert.Equal(1, Faction(game, f).Cp);
    }

    [Fact]
    public void MAP_012_the_exact_cost_is_enough()
    {
        var (game, f) = Setup();
        Faction(game, f).Reserve = 5;
        Faction(game, f).Cp = 2;

        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
        Assert.Equal(0, Faction(game, f).Reserve);
        Assert.Equal(0, Faction(game, f).Cp);
    }

    [Fact]
    public void MAP_025_a_second_upgrade_needs_the_higher_cp()
    {
        var (game, f) = Setup();
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
        Faction(game, f).Cp = 4;   // the second costs 5

        AssertRejected(game, new UpgradeFobCommand(f, "supply-2"), "CP");
    }

    [Theory]
    [InlineData("major-1")]
    [InlineData("command-1")]
    [InlineData("hq-east")]
    [InlineData("hq-west")]
    [InlineData("nowhere")]
    public void MAP_046_only_a_supply_st_can_be_upgraded(string id)
    {
        var (game, f) = Setup();
        if (id != "nowhere") Loc(game, id).Owner = f;

        AssertRejected(game, new UpgradeFobCommand(f, id));
    }

    [Fact]
    public void MAP_046_an_fob_cannot_be_upgraded_again()
    {
        var (game, f) = Setup();
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);

        AssertRejected(game, new UpgradeFobCommand(f, "supply-1"));
    }

    [Fact]
    public void MAP_012_a_supply_st_owned_by_another_faction_or_nobody_cannot_be_upgraded()
    {
        var (game, f) = Setup();
        Loc(game, "supply-2").Owner = Other(f);
        Loc(game, "supply-3").Owner = null;

        AssertRejected(game, new UpgradeFobCommand(f, "supply-2"));
        AssertRejected(game, new UpgradeFobCommand(f, "supply-3"));
    }

    [Fact]
    public void MAP_007_there_is_no_cap_so_every_upgrade_the_cp_can_pay_for_is_accepted()
    {
        var (game, f) = Setup();
        Faction(game, f).Reserve = 500;
        Faction(game, f).Cp = 2 + 5 + 8;   // exactly three upgrades

        foreach (var id in new[] { "supply-1", "supply-2", "supply-3" })
            Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, id)).Accepted);
        Assert.Equal(0, Faction(game, f).Cp);
        Assert.Equal(3, game.Locations.Count(l => l.Type == LocationType.Fob));
    }

    [Fact]
    public void TRN_006_only_the_active_faction_can_upgrade()
    {
        var (game, f) = Setup();

        AssertRejected(game, new UpgradeFobCommand(Other(f), "supply-1"));
    }

    [Fact]
    public void MAP_008_an_owned_fob_resupplies_a_brigade_within_p_sup_08()
    {
        var (game, f) = Setup();
        var b = Add(game, f, 20, At(3, 1));    // one hex from supply-1
        var far = Add(game, f, 20, At(4, 3));  // not near anything owned
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
        Disown(game, "supply-2", "supply-3");

        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, b.Id, 10)).Accepted);
        Assert.Equal(30, b.SP);
        AssertRejected(game, new DistributeSpCommand(f, far.Id, 10));
    }

    [Fact]
    public void SUP_012_an_fob_owned_by_someone_else_does_not_resupply()
    {
        var (game, f) = Setup();
        var b = Add(game, f, 20, At(3, 1));
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
        Loc(game, "supply-1").Owner = Other(f);

        AssertRejected(game, new DistributeSpCommand(f, b.Id, 10));
    }

    [Fact]
    public void MAP_031_an_fob_pays_p_map_14_in_place_of_the_supply_st_bonus()
    {
        var (game, f) = Setup();
        Disown(game, "supply-2", "supply-3");
        Assert.Equal(5 + 3, game.IncomeRules!.Calculate(game, f).Sp);   // base plus the Supply ST bonus
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);

        Assert.Equal(5 + 7, game.IncomeRules.Calculate(game, f).Sp);    // not 5 + 3 + 7
        Assert.Equal(2, game.IncomeRules.Calculate(game, f).Cp);        // an FOB pays no CP
    }

    [Fact]
    public void TRN_005_the_income_phase_pays_the_fob_to_the_reserve()
    {
        var (game, f) = Setup();
        Disown(game, "supply-2", "supply-3");
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
        var before = Faction(game, f).Reserve;

        EndTurn(game);
        EndTurn(game);

        Assert.Equal(before + 5 + 7, Faction(game, f).Reserve);
    }

    [Fact]
    public void TRN_016_an_fob_pays_nothing_while_an_enemy_brigade_is_on_its_hex()
    {
        var (game, f) = Setup();
        Disown(game, "supply-2", "supply-3");
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
        Add(game, Other(f), 50, At(2, 1));

        Assert.Equal(5, game.IncomeRules!.Calculate(game, f).Sp);
    }

    [Fact]
    public void SUP_027_an_enemy_brigade_on_an_fob_blocks_resupply_from_it_but_not_from_other_locations()
    {
        var (game, f) = Setup();
        Disown(game, "supply-2", "supply-3");
        var near = Add(game, f, 20, At(3, 1));                                  // next to supply-1, the FOB
        var atHq = Add(game, f, 20, game.Brigades.First(b => b.Faction == f).Position);   // on its own HQ hex
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
        Add(game, Other(f), 50, At(2, 1));

        AssertRejected(game, new DistributeSpCommand(f, near.Id, 10));
        Assert.True(game.ApplyCommand(new DistributeSpCommand(f, atHq.Id, 10)).Accepted);
    }

    [Fact]
    public void MAP_027_an_owned_fob_claims_radius_sight_minus_one_and_an_unowned_one_claims_nothing()
    {
        var (game, f) = Setup();
        Disown(game, "supply-2", "supply-3");
        Assert.Equal(3, game.Territory!.RadiusOf(LocationType.Fob));   // sight 4 minus 1
        var before = game.Territory.HexesOf(game, f);
        var fobAt = At(2, 1);

        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
        var after = game.Territory.HexesOf(game, f);

        var added = after.Except(before).ToList();
        Assert.Contains(added, h => h.DistanceTo(fobAt) == 3);        // reaches radius 3
        Assert.All(after.Where(h => !before.Contains(h)), h => Assert.True(h.DistanceTo(fobAt) <= 3));
        Assert.Contains(fobAt, after);

        Loc(game, "supply-1").Owner = null;
        Assert.DoesNotContain(added.First(h => h.DistanceTo(fobAt) == 3), game.Territory.HexesOf(game, f));
    }

    [Fact]
    public void P_FOW_03_the_shipped_data_gives_an_fob_territory_radius_1()
    {
        var game = new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.LoadShipped());
        Assert.Equal(1, game.Territory!.RadiusOf(LocationType.Fob));
    }

    [Fact]
    public void MAP_010_an_enemy_brigade_ending_its_turn_on_an_fob_starts_no_capture()
    {
        var (game, f) = Setup();
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
        EndTurn(game);
        Add(game, Other(f), 50, At(2, 1));
        EndTurn(game);   // the enemy ends its turn on the FOB

        Assert.Null(Loc(game, "supply-1").Capturing);
        EndTurn(game);
        EndTurn(game);
        Assert.Equal(f, Loc(game, "supply-1").Owner);
    }

    [Fact]
    public void MAP_010_upgrading_clears_a_pending_capture_so_the_fob_does_not_change_owner()
    {
        var (game, f) = Setup();
        EndTurn(game);
        Add(game, Other(f), 50, At(2, 1));
        EndTurn(game);   // the enemy ends its turn on supply-1: a capture is pending
        Assert.Equal(Other(f), Loc(game, "supply-1").Capturing);

        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);
        Assert.Null(Loc(game, "supply-1").Capturing);
        Assert.Null(Loc(game, "supply-1").CapturingBrigade);
        EndTurn(game);   // the enemy's next turn starts

        Assert.Equal(f, Loc(game, "supply-1").Owner);
        Assert.Equal(LocationType.Fob, Loc(game, "supply-1").Type);
    }

    [Fact]
    public void MAP_021_a_map_file_cannot_place_an_fob()
    {
        Assert.Throws<MapFormatException>(() => MapImporter.Parse(MapJson.Replace("\"id\":\"supply-3\",\"type\":\"supply\"", "\"id\":\"supply-3\",\"type\":\"fob\"")));
    }

    [Fact]
    public void TEC_008_the_upgrade_count_and_the_fob_are_in_the_hash()
    {
        var (game, f) = Setup();
        var before = game.ComputeHash();
        Assert.DoesNotContain(" fob", game.CanonicalText());
        Assert.DoesNotContain("fobupgrades", game.CanonicalText());

        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, "supply-1")).Accepted);

        Assert.NotEqual(before, game.ComputeHash());
        Assert.Contains("location=supply-1 owner=" + f + " fob\n", game.CanonicalText());
        Assert.Contains("fobupgrades=1\n", game.CanonicalText());
    }

    [Fact]
    public void DLV_013_the_command_round_trips_through_json()
    {
        var registry = CommandRegistry.CreateDefault();
        var c = new UpgradeFobCommand("east", "supply-1");

        var json = registry.WriteCommand(c);

        Assert.Equal("{\"faction\":\"east\",\"location\":\"supply-1\",\"type\":\"upgradeFob\"}", json);
        Assert.Equal(c, registry.ReadCommand(json));
        Assert.Throws<JsonException>(() => registry.ReadCommand("{\"faction\":\"east\",\"type\":\"upgradeFob\"}"));
    }

    [Fact]
    public void DLV_014_a_missing_or_bad_fob_parameter_fails_at_game_start()
    {
        Assert.Throws<MissingParameterException>(() => NewGame(Params.Replace("\nP-MAP-14 = 7", "")));
        Assert.Throws<MissingParameterException>(() => NewGame(Params.Replace("\nP-MAP-03 = sp=5 cp=2 cpStep=3", "")));
        Assert.Throws<ParameterFormatException>(() => NewGame(Params.Replace("cpStep=3", "cpStep=-1")));
    }

    [Fact]
    public void MAP_025_a_cp_step_below_1_fails_at_game_start_naming_the_parameter()
    {
        var zero = Assert.Throws<ParameterFormatException>(() => NewGame(Params.Replace("cpStep=3", "cpStep=0")));
        Assert.Contains("P-MAP-03", zero.Message);
        Assert.Contains("P-MAP-03", Assert.Throws<ParameterFormatException>(() => NewGame(Params.Replace("cpStep=3", "cpStep=-1"))).Message);
        NewGame(Params.Replace("cpStep=3", "cpStep=1"));   // 1 is allowed
    }

    [Fact]
    public void DLV_014_the_fob_parameters_are_placeholders_in_the_shipped_data()
    {
        var shipped = ParameterTable.LoadShipped();
        Assert.True(shipped.IsPlaceholder("P-MAP-03"));
        Assert.True(shipped.IsPlaceholder("P-MAP-14"));
        Assert.Equal(shipped.GetInt("P-RES-04"), shipped.GetInt("P-MAP-14"));   // equal to the Supply ST bonus for now
    }

    [Fact]
    public void TEC_008_replaying_a_log_with_a_capture_and_an_upgrade_gives_the_same_hash()
    {
        var registry = CommandRegistry.CreateDefault();
        var game = NewGame();
        var f = game.ActiveFaction!;
        // The first player walks onto a Supply ST that is two hexes from its HQ in a straight line, then holds it a turn.
        var (st, path) = f == "east"
            ? ("supply-3", new[] { At(8, 1), At(7, 1) })
            : ("supply-4", new[] { At(0, 1), At(0, 0) });
        var brigade = game.Brigades.Single(b => b.Faction == f);
        Assert.True(game.ApplyCommand(new MoveBrigadeCommand(f, brigade.Id, path)).Accepted);
        EndTurn(game);
        EndTurn(game);
        Assert.Equal(f, Loc(game, st).Owner);   // the capture completed at the start of the turn
        Assert.True(game.ApplyCommand(new UpgradeFobCommand(f, st)).Accepted, "upgrade");

        var run = registry.ReadLog(registry.WriteLog(7, game.Log));
        var replayed = GameState.Replay(run.Seed, run.Log, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(Params));

        Assert.Equal(game.ComputeHash(), replayed.ComputeHash());
        Assert.Equal(LocationType.Fob, Loc(replayed, st).Type);
    }
}
