using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class IncomeTests
{
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{\"infantry\":{\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    private const string MapJson =
        "{\"version\":1,\"width\":8,\"height\":4,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[7,1],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,2],\"faction\":\"west\"}," +
        "{\"id\":\"supply-1\",\"type\":\"supply\",\"at\":[2,1]}," +
        "{\"id\":\"command-1\",\"type\":\"command\",\"at\":[3,1]}," +
        "{\"id\":\"major-1\",\"type\":\"major\",\"at\":[4,1]}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}}";

    // Base SP 5, base CP 2, ST bonus 3: every amount a test checks has its own value.
    private const string Params = "P-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 5\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2";

    private static GameState NewGame(string parameters = Params)
        => new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(parameters));

    private static FactionState Faction(GameState g, string id) => g.Factions.Single(f => f.Id == id);
    private static LocationState Location(GameState g, string id) => g.Locations.Single(l => l.Id == id);
    private static string Other(string f) => f == "east" ? "west" : "east";
    private static IncomeAmount IncomeOf(GameState g, string f) => g.IncomeRules!.Calculate(g, f);

    private static void EndTurn(GameState g)
        => Assert.True(g.ApplyCommand(new EndTurnCommand(g.ActiveFaction)).Accepted);

    /// <summary>Ends turns until it is <paramref name="faction"/>'s turn again, so its income has just been paid.</summary>
    private static void PlayRound(GameState g, string faction)
    {
        do EndTurn(g); while (g.ActiveFaction != faction);
    }

    [Fact]
    public void RES_001_sp_is_held_in_one_reserve_on_the_faction()
    {
        var game = NewGame();
        var first = game.ActiveFaction!;

        Assert.Equal(5 + 5, Faction(game, first).Reserve);
        Assert.Equal(typeof(int), typeof(FactionState).GetProperty("Reserve")!.PropertyType);
        Assert.Null(typeof(Section).GetProperty("SP")); // Sections hold no SP (BRG-018)
    }

    [Fact]
    public void TRN_001_only_the_active_faction_is_paid_in_its_income_step()
    {
        var game = NewGame();
        var first = game.ActiveFaction!;
        var second = Other(first);

        // Turn 1: the first player was paid inside the constructor, the other has nothing yet.
        Assert.Equal(2, Faction(game, first).Cp);
        Assert.Equal(0, Faction(game, second).Cp);
        Assert.Equal(5, Faction(game, second).Reserve);

        var firstReserve = Faction(game, first).Reserve;
        EndTurn(game);

        Assert.Equal(firstReserve, Faction(game, first).Reserve);
        Assert.Equal(2, Faction(game, first).Cp);
        Assert.Equal(2, Faction(game, second).Cp);
        Assert.Equal(5 + 5, Faction(game, second).Reserve);
    }

    [Fact]
    public void TRN_001_income_is_paid_once_per_turn_and_not_by_other_commands()
    {
        var game = NewGame();
        var active = Faction(game, game.ActiveFaction!);
        var reserve = active.Reserve;

        Assert.True(game.ApplyCommand(new AddToTallyCommand(1, game.ActiveFaction)).Accepted);

        Assert.Equal(reserve, active.Reserve);
        PlayRound(game, game.ActiveFaction!);
        Assert.Equal(reserve + 5, active.Reserve);
        Assert.Equal(4, active.Cp);
    }

    [Fact]
    public void RES_002_sp_income_with_no_sts_is_the_base_and_with_sts_adds_their_bonuses()
    {
        var game = NewGame();
        Assert.Equal(5, IncomeOf(game, "east").Sp);

        Location(game, "supply-1").Owner = "east";
        Assert.Equal(8, IncomeOf(game, "east").Sp);

        Location(game, "major-1").Owner = "east";
        Assert.Equal(11, IncomeOf(game, "east").Sp);
        Assert.Equal(5, IncomeOf(game, "west").Sp); // the other Faction does not gain from them
    }

    [Fact]
    public void RES_005_cp_income_with_no_sts_is_the_base_and_with_sts_adds_their_bonuses()
    {
        var game = NewGame();
        Assert.Equal(2, IncomeOf(game, "east").Cp);

        Location(game, "command-1").Owner = "east";
        Assert.Equal(5, IncomeOf(game, "east").Cp);

        Location(game, "major-1").Owner = "east";
        Assert.Equal(8, IncomeOf(game, "east").Cp);
    }

    [Fact]
    public void RES_010_a_supply_st_adds_sp_only()
    {
        var game = NewGame();
        Location(game, "supply-1").Owner = "west";

        Assert.Equal(8, IncomeOf(game, "west").Sp);
        Assert.Equal(2, IncomeOf(game, "west").Cp);
    }

    [Fact]
    public void RES_010_a_command_st_adds_cp_only()
    {
        var game = NewGame();
        Location(game, "command-1").Owner = "west";

        Assert.Equal(5, IncomeOf(game, "west").Sp);
        Assert.Equal(5, IncomeOf(game, "west").Cp);
    }

    [Fact]
    public void RES_010_a_major_st_adds_both()
    {
        var game = NewGame();
        Location(game, "major-1").Owner = "west";

        Assert.Equal(8, IncomeOf(game, "west").Sp);
        Assert.Equal(5, IncomeOf(game, "west").Cp);
    }

    [Fact]
    public void MAP_022_the_income_types_are_paid_into_the_reserve_and_cp_total_at_the_turn()
    {
        var game = NewGame();
        var f = game.ActiveFaction!;
        Location(game, "supply-1").Owner = f;
        Location(game, "command-1").Owner = f;
        Location(game, "major-1").Owner = f;
        var state = Faction(game, f);
        var (reserve, cp) = (state.Reserve, state.Cp);

        PlayRound(game, f);

        Assert.Equal(reserve + 5 + 3 + 3, state.Reserve);
        Assert.Equal(cp + 2 + 3 + 3, state.Cp);
    }

    [Fact]
    public void MAP_001_a_neutral_st_adds_nothing_and_an_owned_one_adds_every_turn_while_held()
    {
        var game = NewGame();
        var f = game.ActiveFaction!;
        var state = Faction(game, f);
        foreach (var id in new[] { "supply-1", "command-1", "major-1" })
            Assert.Null(Location(game, id).Owner);
        Assert.Equal(5, IncomeOf(game, f).Sp);
        Assert.Equal(5, IncomeOf(game, Other(f)).Sp);
        Assert.Equal(2, IncomeOf(game, f).Cp);
        Assert.Equal(2, IncomeOf(game, Other(f)).Cp);

        Location(game, "supply-1").Owner = f;
        var before = state.Reserve;
        PlayRound(game, f);
        Assert.Equal(before + 8, state.Reserve);
        PlayRound(game, f);
        Assert.Equal(before + 16, state.Reserve);

        Location(game, "supply-1").Owner = null; // no longer held
        PlayRound(game, f);
        Assert.Equal(before + 21, state.Reserve);
    }

    [Fact]
    public void MAP_024_a_faction_with_no_sts_still_receives_the_hq_income()
    {
        var game = NewGame();
        var f = game.ActiveFaction!;
        var state = Faction(game, f);
        Location(game, "supply-1").Owner = Other(f);
        Location(game, "command-1").Owner = Other(f);
        Location(game, "major-1").Owner = Other(f);
        var (reserve, cp) = (state.Reserve, state.Cp);

        PlayRound(game, f);

        Assert.Equal(reserve + 5, state.Reserve);
        Assert.Equal(cp + 2, state.Cp);
    }

    [Fact]
    public void RES_011_a_faction_that_has_lost_all_its_sts_can_still_recover()
    {
        var game = NewGame();
        var f = game.ActiveFaction!;
        Location(game, "major-1").Owner = f;
        Assert.Equal(8, IncomeOf(game, f).Sp);

        Location(game, "major-1").Owner = Other(f); // captured by the enemy

        Assert.Equal(5, IncomeOf(game, f).Sp);
        Assert.Equal(2, IncomeOf(game, f).Cp);
    }

    [Fact]
    public void TRN_016_an_st_with_an_enemy_brigade_on_its_hex_pays_its_owner_nothing()
    {
        var game = NewGame();
        var f = game.ActiveFaction!;
        var st = Location(game, "major-1");
        st.Owner = f;
        var enemy = game.Brigades.Single(b => b.Faction == Other(f));

        enemy.Position = st.Position;
        Assert.Equal(5, IncomeOf(game, f).Sp);
        Assert.Equal(2, IncomeOf(game, f).Cp);

        enemy.Position = HexCoord.FromOffset(7, 3); // the enemy has gone
        Assert.Equal(8, IncomeOf(game, f).Sp);
        Assert.Equal(5, IncomeOf(game, f).Cp);
    }

    [Fact]
    public void TRN_016_the_blocked_st_pays_nothing_in_the_turn_and_pays_again_after()
    {
        var game = NewGame();
        var f = game.ActiveFaction!;
        var state = Faction(game, f);
        var st = Location(game, "supply-1");
        st.Owner = f;
        var enemy = game.Brigades.Single(b => b.Faction == Other(f));
        enemy.Position = st.Position;
        var before = state.Reserve;

        PlayRound(game, f);
        Assert.Equal(before + 5, state.Reserve);

        enemy.Position = HexCoord.FromOffset(7, 3);
        PlayRound(game, f);
        Assert.Equal(before + 5 + 8, state.Reserve);
    }

    [Fact]
    public void TRN_016_the_owners_own_brigade_on_its_st_does_not_block_it()
    {
        var game = NewGame();
        var f = game.ActiveFaction!;
        var st = Location(game, "supply-1");
        st.Owner = f;
        game.Brigades.Single(b => b.Faction == f).Position = st.Position;

        Assert.Equal(8, IncomeOf(game, f).Sp);
    }

    [Fact]
    public void TRN_016_an_enemy_brigade_on_the_hq_hex_does_not_stop_the_guaranteed_income()
    {
        var game = NewGame();
        var f = game.ActiveFaction!;
        game.Brigades.Single(b => b.Faction == Other(f)).Position = Location(game, "hq-" + f).Position;

        Assert.Equal(5, IncomeOf(game, f).Sp);
        Assert.Equal(2, IncomeOf(game, f).Cp);
    }

    [Theory]
    [InlineData("P-RES-01")]
    [InlineData("P-RES-02")]
    [InlineData("P-RES-04")]
    public void DLV_014_a_missing_income_parameter_is_an_error_not_a_default(string missing)
    {
        var parameters = string.Join("\n", Params.Split('\n').Where(l => !l.StartsWith(missing)));

        var ex = Assert.Throws<MissingParameterException>(() => NewGame(parameters));
        Assert.Equal(missing, ex.ParameterId);
        Assert.Throws<MissingParameterException>(() => new IncomeRules(ParameterTable.Parse(parameters)));
    }

    [Fact]
    public void DLV_014_a_negative_income_value_is_rejected()
    {
        NewGame();

        Assert.Throws<ParameterFormatException>(() => NewGame(Params.Replace("P-RES-04 = 3", "P-RES-04 = -1")));
    }

    [Fact]
    public void DLV_014_the_shipped_income_values_are_placeholders_and_the_duplicate_p_map_values_are_gone()
    {
        var shipped = ParameterTable.LoadShipped();

        foreach (var id in new[] { "P-RES-01", "P-RES-02", "P-RES-04" })
            Assert.True(shipped.IsPlaceholder(id), id);
        Assert.DoesNotContain("P-MAP-05", shipped.Ids);
        Assert.DoesNotContain("P-MAP-06", shipped.Ids);
        Assert.NotNull(new IncomeRules(shipped));
    }

    [Fact]
    public void TEC_008_cp_is_part_of_the_hash_and_replay_reproduces_income()
    {
        var game = NewGame();
        var hash = game.ComputeHash();
        Faction(game, "east").Cp += 1;
        Assert.NotEqual(hash, game.ComputeHash());

        var played = NewGame();
        PlayRound(played, played.ActiveFaction!);
        var replayed = GameState.Replay(7, played.Log, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(Params));
        Assert.Equal(played.ComputeHash(), replayed.ComputeHash());
        Assert.Equal(played.CanonicalText(), replayed.CanonicalText());
    }
}
