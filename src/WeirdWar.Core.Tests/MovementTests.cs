using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class MovementTests
{
    // 4 columns x 10 rows. West HQ at (0,0), east HQ at (3,9). Going down a column is one hex per row.
    // Seed 7 gives west the first turn (see TurnTests). Every Section here costs 1 SP per hex, so a Brigade costs 3 per hex.
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{" +
        "\"infantry\":{\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}," +
        "\"thrall\":{\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1,\"slow\":true}}}";

    private static string MapJson(string westSections = "\"infantry\",\"infantry\",\"infantry\"") =>
        "{\"version\":1,\"width\":4,\"height\":10," +
        "\"terrain\":{\"mountain\":[[1,0]],\"sea\":[[0,6]],\"river\":[[2,2]]}," +
        "\"factions\":[\"east\",\"west\"]," +
        "\"locations\":[{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[3,9],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,0],\"faction\":\"west\"}]," +
        "\"setup\":{" +
        "\"east\":{\"reserve\":50,\"brigade\":{\"sp\":90,\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"reserve\":50,\"brigade\":{\"sp\":90,\"sections\":[" + westSections + "]}}}}";

    private const string Params = "P-BRG-05 = 3\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2";

    private static GameState NewGame(string westSections = "infantry,infantry,infantry", string parameters = Params)
    {
        var quoted = string.Join(",", westSections.Split(',').Select(t => "\"" + t + "\""));
        var game = new GameState(7, MapImporter.Parse(MapJson(quoted)), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(parameters));
        Assert.Equal("west", game.ActiveFaction);
        return game;
    }

    private static Brigade West(GameState game) => game.Brigades.First(b => b.Faction == "west");
    private static Brigade East(GameState game) => game.Brigades.First(b => b.Faction == "east");

    private static HexCoord At(int c, int r) => HexCoord.FromOffset(c, r);

    // The hexes (0,1), (0,2), ... down the west column, n of them.
    private static HexCoord[] Down(int n, int fromRow = 0) => Enumerable.Range(fromRow + 1, n).Select(r => At(0, r)).ToArray();

    private static CommandResult Move(GameState game, int n, int fromRow = 0)
        => game.ApplyCommand(new MoveBrigadeCommand("west", West(game).Id, Down(n, fromRow)));

    private static void EndBothTurns(GameState game)
    {
        Assert.True(game.ApplyCommand(new EndTurnCommand("west")).Accepted);
        Assert.True(game.ApplyCommand(new EndTurnCommand("east")).Accepted);
    }

    // ---- SUP-005, RES-004

    [Fact]
    public void SUP_005_a_brigade_with_enough_sp_moves_and_spends_sp_per_hex()
    {
        var game = NewGame();

        Assert.True(Move(game, 4).Accepted);

        Assert.Equal(90 - 4 * 3, West(game).SP);
        Assert.Equal(At(0, 4), West(game).Position);
    }

    [Fact]
    public void RES_004_moving_spends_sp()
    {
        var game = NewGame();
        var before = West(game).SP;

        Move(game, 1);

        Assert.True(West(game).SP < before);
    }

    [Fact]
    public void SUP_005_a_brigade_can_move_as_many_hexes_as_its_sp_allows_and_no_further()
    {
        var game = NewGame();
        West(game).SP = 7; // 3 per hex: two hexes fit, three do not

        Assert.False(Move(game, 3).Accepted);
        Assert.True(Move(game, 2).Accepted);
        Assert.Equal(1, West(game).SP);
        Assert.Equal(At(0, 2), West(game).Position);
    }

    [Fact]
    public void SUP_005_a_move_that_uses_exactly_all_the_sp_is_accepted_and_leaves_zero()
    {
        var game = NewGame();
        West(game).SP = 6;

        Assert.True(Move(game, 2).Accepted);
        Assert.Equal(0, West(game).SP);
    }

    // ---- SUP-015

    [Fact]
    public void SUP_015_a_brigade_with_zero_sp_cannot_move_even_with_a_full_reserve()
    {
        var game = NewGame();
        West(game).SP = 0;
        var reserve = game.Factions.Single(f => f.Id == "west").Reserve;
        var hash = game.ComputeHash();

        var result = Move(game, 1);

        Assert.False(result.Accepted);
        Assert.Contains("SUP-015", result.Reason);
        Assert.Equal(reserve, game.Factions.Single(f => f.Id == "west").Reserve);
        Assert.Equal(0, West(game).SP);
        Assert.Equal(hash, game.ComputeHash());
    }

    [Fact]
    public void SUP_015_a_successful_move_never_touches_the_reserve()
    {
        var game = NewGame();
        var reserve = game.Factions.Single(f => f.Id == "west").Reserve;

        Move(game, 3);

        Assert.Equal(reserve, game.Factions.Single(f => f.Id == "west").Reserve);
    }

    // ---- SUP-006

    [Fact]
    public void SUP_006_a_move_that_would_take_sp_below_zero_is_rejected_and_nothing_changes()
    {
        var game = NewGame();
        West(game).SP = 5;
        var hash = game.ComputeHash();

        var result = Move(game, 2); // costs 6

        Assert.False(result.Accepted);
        Assert.Contains("SUP-006", result.Reason);
        Assert.Equal(5, West(game).SP);
        Assert.Equal(At(0, 0), West(game).Position);
        Assert.Equal(0, game.Log.Count);
        Assert.Equal(hash, game.ComputeHash());
    }

    [Fact]
    public void SUP_006_sp_never_goes_negative_through_movement()
    {
        var game = NewGame();
        West(game).SP = 10;
        for (var i = 0; i < 5; i++) Move(game, 1, i);

        Assert.True(West(game).SP >= 0);
        Assert.Equal(At(0, 3), West(game).Position);
    }

    // ---- TRN-029, TRN-030, TRN-032

    [Fact]
    public void TRN_029_sp_is_deducted_as_each_hex_is_moved_not_at_the_end_of_the_turn()
    {
        var game = NewGame();

        Move(game, 1);
        Assert.Equal(87, West(game).SP);
        Move(game, 1, 1);
        Assert.Equal(84, West(game).SP);

        Assert.Equal(2, West(game).HexesMovedIn(game.TurnNumber));
        Assert.Equal(TurnPhase.Actions, game.Phase); // still mid-turn: nothing waits for the end of the turn
    }

    [Fact]
    public void TRN_030_a_brigade_can_move_again_after_sp_is_added_in_the_same_turn()
    {
        var game = NewGame();
        West(game).SP = 6;
        Assert.True(Move(game, 2).Accepted);
        Assert.False(Move(game, 1, 2).Accepted); // 0 SP

        West(game).SP += 9; // stands in for a distribution (CC1-74), which is not built yet
        Assert.True(Move(game, 3, 2).Accepted);

        Assert.Equal(At(0, 5), West(game).Position);
    }

    [Fact]
    public void TRN_030_a_brigade_can_move_again_after_doing_nothing_and_sp_is_the_only_limit()
    {
        var game = NewGame();
        Assert.True(Move(game, 1).Accepted);
        Assert.True(game.ApplyCommand(new AddToTallyCommand(1, "west")).Accepted); // another action in between
        Assert.True(Move(game, 1, 1).Accepted);
        Assert.True(Move(game, 1, 2).Accepted);

        Assert.Equal(90 - 9, West(game).SP);
    }

    [Fact]
    public void TRN_032_a_brigade_in_a_new_game_can_move_straight_away()
    {
        var game = NewGame();

        Assert.Equal(0, game.Log.Count);
        Assert.True(Move(game, 1).Accepted);
    }

    [Fact]
    public void TRN_032_a_brigade_added_to_the_state_this_turn_can_move_at_once()
    {
        var game = NewGame();
        var added = new Brigade(3, "west", 40, At(2, 0), new List<Section> { new Section("infantry", 10) });
        ((List<Brigade>)game.Brigades).Add(added);

        var result = game.ApplyCommand(new MoveBrigadeCommand("west", 3, new[] { At(2, 1) }));

        Assert.True(result.Accepted);
        Assert.Equal(39, added.SP); // one Section, 1 SP a hex
    }

    // ---- SUP-024, TAG-010, SUP-025

    [Fact]
    public void SUP_024_a_brigade_without_a_slow_section_has_no_hexes_per_turn_cap()
    {
        var game = NewGame();

        Assert.True(Move(game, 5).Accepted); // P-SUP-10 is 2 in these tests

        Assert.Equal(5, West(game).HexesMovedIn(game.TurnNumber));
    }

    [Fact]
    public void TAG_010_a_slow_brigade_cannot_move_more_than_the_cap_in_one_turn()
    {
        var game = NewGame("thrall,thrall,thrall");

        Assert.False(Move(game, 3).Accepted);
        Assert.True(Move(game, 2).Accepted);
        Assert.Equal(At(0, 2), West(game).Position);
    }

    [Fact]
    public void SUP_024_the_slow_cap_is_a_total_across_split_moves()
    {
        var game = NewGame("thrall,thrall,thrall");

        Assert.True(Move(game, 1).Accepted);
        var tooFar = Move(game, 2, 1);
        Assert.False(tooFar.Accepted);
        Assert.Contains("P-SUP-10", tooFar.Reason);
        Assert.True(Move(game, 1, 1).Accepted);
        Assert.False(Move(game, 1, 2).Accepted);
    }

    [Fact]
    public void SUP_024_the_slow_cap_starts_afresh_each_turn()
    {
        var game = NewGame("thrall,thrall,thrall");
        Assert.True(Move(game, 2).Accepted);
        Assert.False(Move(game, 1, 2).Accepted);

        EndBothTurns(game);

        Assert.True(Move(game, 2, 2).Accepted);
        Assert.Equal(At(0, 4), West(game).Position);
    }

    [Fact]
    public void SUP_024_split_moves_in_a_later_turn_are_counted_from_zero_and_capped_in_total()
    {
        // Turn 1 uses the whole allowance; turn 3 (west's second turn) must start from 0 and then add up across two moves.
        var game = NewGame("thrall,thrall,thrall");
        Assert.True(Move(game, 2).Accepted);
        EndBothTurns(game);
        Assert.Equal(3, game.TurnNumber);

        Assert.True(Move(game, 1, 2).Accepted);
        Assert.Equal(1, West(game).HexesMovedIn(3));
        Assert.True(Move(game, 1, 3).Accepted);
        Assert.Equal(2, West(game).HexesMovedIn(3));
        Assert.False(Move(game, 1, 4).Accepted);
        Assert.Equal(2, West(game).HexesMovedIn(3));
        Assert.Equal(0, West(game).HexesMovedIn(1 + 1)); // not the other Faction's turn number
    }

    [Fact]
    public void SUP_024_a_rejected_move_does_not_use_up_the_slow_allowance()
    {
        var game = NewGame("thrall,thrall,thrall");
        Assert.False(Move(game, 3).Accepted);

        Assert.Equal(0, West(game).HexesMovedIn(game.TurnNumber));
        Assert.True(Move(game, 2).Accepted);
    }

    [Fact]
    public void SUP_025_a_brigade_with_slow_and_other_sections_is_limited_by_the_slow_section()
    {
        var game = NewGame("infantry,infantry,thrall");

        Assert.False(Move(game, 3).Accepted);
        Assert.True(Move(game, 2).Accepted);
    }

    // ---- TRN-020, TRN-023, TRN-024

    private static void PutEastAt(GameState game, HexCoord hex) => East(game).Position = hex;

    [Fact]
    public void TRN_020_a_brigade_cannot_end_its_move_on_a_hex_held_by_an_enemy_brigade()
    {
        var game = NewGame();
        PutEastAt(game, At(0, 3));

        Assert.False(Move(game, 3).Accepted);
    }

    [Fact]
    public void TRN_020_a_brigade_cannot_end_its_move_on_a_hex_held_by_a_friendly_brigade()
    {
        var game = NewGame();
        ((List<Brigade>)game.Brigades).Add(new Brigade(3, "west", 40, At(0, 3), new List<Section> { new Section("infantry", 10) }));
        var hash = game.ComputeHash();

        var result = Move(game, 3);

        Assert.False(result.Accepted);
        Assert.Contains("TRN-020", result.Reason);
        Assert.Equal(hash, game.ComputeHash());
    }

    [Fact]
    public void TRN_023_a_brigade_may_pass_through_a_friendly_brigade_but_not_end_on_it()
    {
        // TRN-023 lists only enemy Brigades as blocking; TRN-020 only forbids sharing a hex, i.e. ending on one.
        var game = NewGame();
        ((List<Brigade>)game.Brigades).Add(new Brigade(3, "west", 40, At(0, 2), new List<Section> { new Section("infantry", 10) }));

        Assert.False(Move(game, 2).Accepted); // ends on the friendly Brigade
        Assert.True(Move(game, 4).Accepted);  // passes through it
        Assert.Equal(At(0, 4), West(game).Position);
        Assert.Equal(At(0, 2), game.Brigades.Single(b => b.Id == 3).Position);
    }

    [Fact]
    public void TRN_023_a_hex_held_by_an_enemy_brigade_cannot_be_entered_even_part_way_along_the_path()
    {
        var game = NewGame();
        PutEastAt(game, At(0, 2));

        var hash = game.ComputeHash();
        var result = Move(game, 4);

        Assert.False(result.Accepted);
        Assert.Contains("enemy", result.Reason);
        Assert.Equal(hash, game.ComputeHash());
        Assert.False(Move(game, 2).Accepted);
        Assert.True(Move(game, 1).Accepted); // the hex before the enemy is fine
    }

    [Fact]
    public void TRN_023_a_hex_beyond_the_map_edge_cannot_be_entered()
    {
        var game = NewGame();

        var result = game.ApplyCommand(new MoveBrigadeCommand("west", West(game).Id, new[] { new HexCoord(0, -1) }));

        Assert.False(result.Accepted);
        Assert.Contains("map edge", result.Reason);
    }

    [Fact]
    public void TRN_023_impassable_terrain_cannot_be_entered()
    {
        var game = NewGame();

        Assert.False(game.ApplyCommand(new MoveBrigadeCommand("west", West(game).Id, new[] { At(1, 0) })).Accepted);
    }

    [Theory]
    [InlineData(1, 0, 0, 0, "mountain")]
    [InlineData(0, 6, 0, 5, "sea")]
    [InlineData(2, 2, 2, 1, "river")]
    public void TRN_024_mountain_sea_and_river_hexes_are_rejected(int tc, int tr, int fromC, int fromR, string terrain)
    {
        var game = NewGame();
        West(game).Position = At(fromC, fromR);

        var result = game.ApplyCommand(new MoveBrigadeCommand("west", West(game).Id, new[] { At(tc, tr) }));

        Assert.False(result.Accepted);
        Assert.Contains(terrain, result.Reason);
        Assert.Equal(At(fromC, fromR), West(game).Position);
    }

    // ---- Path shape and ownership (TRN-002)

    [Fact]
    public void TRN_002_a_path_that_skips_a_hex_is_rejected()
    {
        var game = NewGame();

        Assert.False(game.ApplyCommand(new MoveBrigadeCommand("west", West(game).Id, new[] { At(0, 2) })).Accepted);
    }

    [Fact]
    public void TRN_002_an_empty_path_an_unknown_brigade_and_someone_elses_brigade_are_rejected()
    {
        var game = NewGame();

        Assert.False(game.ApplyCommand(new MoveBrigadeCommand("west", West(game).Id, new HexCoord[0])).Accepted);
        Assert.False(game.ApplyCommand(new MoveBrigadeCommand("west", 99, Down(1))).Accepted);
        Assert.False(game.ApplyCommand(new MoveBrigadeCommand("west", East(game).Id, Down(1))).Accepted);
    }

    [Fact]
    public void TRN_006_only_the_active_faction_can_move()
    {
        var game = NewGame();

        var result = game.ApplyCommand(new MoveBrigadeCommand("east", East(game).Id, new[] { At(3, 8) }));

        Assert.False(result.Accepted);
        Assert.Equal(At(3, 9), East(game).Position);
    }

    [Fact]
    public void TRN_002_the_active_faction_moves_in_the_actions_phase_and_the_other_after_the_turn_passes()
    {
        var game = NewGame();
        Assert.True(Move(game, 1).Accepted);
        Assert.True(game.ApplyCommand(new EndTurnCommand("west")).Accepted);

        Assert.True(game.ApplyCommand(new MoveBrigadeCommand("east", East(game).Id, new[] { At(3, 8) })).Accepted);
        Assert.Equal(At(3, 8), East(game).Position);
    }

    [Fact]
    public void TRN_005_the_move_command_names_the_actions_phase()
    {
        Assert.Equal(TurnPhase.Actions, new MoveBrigadeCommand("west", 1, Down(1)).RequiredPhase);
    }

    [Fact]
    public void TRN_002_a_state_without_a_campaign_rejects_a_move()
    {
        var state = new GameState(1);

        Assert.False(state.ApplyCommand(new MoveBrigadeCommand("", 1, Down(1))).Accepted);
    }

    [Fact]
    public void TRN_002_every_kind_of_rejected_move_leaves_the_hash_unchanged()
    {
        var game = NewGame();
        PutEastAt(game, At(0, 4));
        var hash = game.ComputeHash();
        var west = West(game).Id;
        var rejected = new ICommand[]
        {
            new MoveBrigadeCommand("west", west, new[] { At(0, 1), At(0, 2), At(0, 4) }),             // not adjacent, part-way along
            new MoveBrigadeCommand("west", west, new[] { At(0, 1), At(0, 2), At(0, 3), At(0, 4) }),   // enemy at the end
            new MoveBrigadeCommand("west", west, new[] { At(0, 1), At(1, 0) }),                       // not adjacent to (0,1)
            new MoveBrigadeCommand("west", west, new[] { At(0, 1), new HexCoord(5, 5) }),             // off the map after a good hex
            new MoveBrigadeCommand("west", west, new[] { At(1, 0) }),                                  // mountain
            new MoveBrigadeCommand("west", 99, new[] { At(0, 1) }),                                    // unknown Brigade
            new MoveBrigadeCommand("west", East(game).Id, new[] { At(3, 8) }),                         // not theirs
            new MoveBrigadeCommand("west", west, new HexCoord[0]),                                     // empty
            new MoveBrigadeCommand("east", East(game).Id, new[] { At(0, 5) }),                         // not the active Faction
        };

        foreach (var command in rejected)
        {
            var result = game.ApplyCommand(command);
            Assert.False(result.Accepted, command.ToString());
            Assert.Equal(hash, game.ComputeHash());
        }
    }

    // ---- TAG-010 data

    [Fact]
    public void TAG_010_the_slow_flag_is_read_from_the_section_data_and_defaults_to_false()
    {
        var types = SectionTypeTable.Parse(TypesJson);

        Assert.True(types.Get("thrall").Slow);
        Assert.False(types.Get("infantry").Slow);
    }

    [Theory]
    [InlineData("\"slow\":1")]
    [InlineData("\"slow\":\"yes\"")]
    [InlineData("\"slow\":null")]
    public void TAG_010_a_slow_value_that_is_not_true_or_false_is_rejected(string slow)
    {
        var json = "{\"version\":1,\"sectionTypes\":{\"x\":{\"health\":1,\"sight\":1,\"capacityContribution\":1,\"consumptionPerHex\":1," + slow + "}}}";

        Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(json));
    }

    [Fact]
    public void P_SUP_10_the_cap_comes_from_the_parameter_table_and_is_a_marked_placeholder()
    {
        var shipped = ParameterTable.LoadShipped();

        Assert.True(shipped.IsPlaceholder("P-SUP-10"));
        Assert.True(shipped.GetInt("P-SUP-10") > 0);

        var game = NewGame("thrall,thrall,thrall", Params.Replace("P-SUP-10 = 2", "P-SUP-10 = 4"));
        Assert.True(Move(game, 4).Accepted);
        Assert.False(Move(game, 1, 4).Accepted);
    }

    [Fact]
    public void P_SUP_10_a_missing_value_fails_when_the_game_starts_not_during_a_move()
    {
        var withoutCap = Params.Replace("\nP-SUP-10 = 2", "");

        Assert.Throws<MissingParameterException>(() => NewGame("thrall,thrall,thrall", withoutCap));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("many")]
    public void P_SUP_10_a_cap_below_one_or_not_a_number_fails_when_the_game_starts(string value)
    {
        Assert.Throws<ParameterFormatException>(() => NewGame("infantry,infantry,infantry", Params.Replace("P-SUP-10 = 2", "P-SUP-10 = " + value)));
    }

    [Fact]
    public void P_SUP_10_the_game_exposes_its_supply_and_movement_rules_built_at_start()
    {
        var game = NewGame();

        Assert.Equal(2, game.MovementRules!.SlowCap);
        Assert.Equal(3, game.Supply!.CostPerHexOf(West(game)));
    }

    [Fact]
    public void SUP_005_a_section_type_that_costs_no_sp_to_move_is_rejected_by_the_data_loader()
    {
        var json = "{\"version\":1,\"sectionTypes\":{\"x\":{\"health\":1,\"sight\":1,\"capacityContribution\":1,\"consumptionPerHex\":0}}}";

        var e = Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(json));
        Assert.Contains("SUP-005", e.Message);
    }

    // ---- TEC-008

    [Fact]
    public void TEC_008_a_move_command_round_trips_through_json_and_equals_its_copy()
    {
        var registry = CommandRegistry.CreateDefault();
        var command = new MoveBrigadeCommand("west", 2, new[] { At(0, 1), At(1, 1), At(1, 2) });

        var copy = registry.ReadCommand(registry.WriteCommand(command));

        Assert.Equal(command, copy);
        Assert.Equal(command.GetHashCode(), copy.GetHashCode());
        Assert.Equal(registry.WriteCommand(command), registry.WriteCommand(copy));
        Assert.NotEqual(command, new MoveBrigadeCommand("west", 2, new[] { At(0, 1), At(1, 1) }));
    }

    [Theory]
    [InlineData("{\"type\":\"moveBrigade\",\"path\":[[0,1]]}")]
    [InlineData("{\"type\":\"moveBrigade\",\"brigade\":1}")]
    [InlineData("{\"type\":\"moveBrigade\",\"brigade\":1,\"path\":[[0]]}")]
    [InlineData("{\"type\":\"moveBrigade\",\"brigade\":1,\"path\":[[0,\"a\"]]}")]
    [InlineData("{\"type\":\"moveBrigade\",\"brigade\":1,\"path\":[[-1,0]]}")]
    [InlineData("{\"type\":\"moveBrigade\",\"brigade\":1,\"path\":\"x\"}")]
    public void TEC_008_a_malformed_move_command_is_a_json_error(string json)
    {
        Assert.Throws<JsonException>(() => CommandRegistry.CreateDefault().ReadCommand(json));
    }

    [Fact]
    public void TEC_008_replaying_a_log_with_moves_reproduces_the_hash_including_the_slow_count()
    {
        var game = NewGame("thrall,thrall,thrall");
        Assert.True(Move(game, 1).Accepted);
        Assert.True(Move(game, 1, 1).Accepted);
        Assert.False(Move(game, 1, 2).Accepted); // cap reached; replay must reach the same state
        var registry = CommandRegistry.CreateDefault();

        var run = registry.ReadLog(registry.WriteLog(7, game.Log));
        var replayed = GameState.Replay(run.Seed, run.Log, MapImporter.Parse(MapJson("\"thrall\",\"thrall\",\"thrall\"")), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(Params));

        Assert.Equal(game.ComputeHash(), replayed.ComputeHash());
        Assert.Equal(2, West(replayed).HexesMovedIn(replayed.TurnNumber));
    }

    [Fact]
    public void TEC_008_a_moved_brigade_changes_the_hash_and_the_text_gets_a_moved_field()
    {
        var game = NewGame();
        var before = game.ComputeHash();

        Move(game, 2);

        Assert.NotEqual(before, game.ComputeHash());
        Assert.Contains("at=0,2 moved=1:2\n", game.CanonicalText());
        Assert.DoesNotContain("moved=", NewGame().CanonicalText());
    }

    [Fact]
    public void TEC_008_the_parameter_values_are_part_of_the_hash_but_comments_and_order_are_not()
    {
        var baseline = NewGame().ComputeHash();
        var otherCap = NewGame("infantry,infantry,infantry", Params.Replace("P-SUP-10 = 2", "P-SUP-10 = 3")).ComputeHash();
        var reordered = string.Join("\n", Params.Split('\n').Reverse()) + " # a comment";

        Assert.NotEqual(baseline, otherCap);
        Assert.Equal(baseline, NewGame("infantry,infantry,infantry", reordered).ComputeHash());
        Assert.Contains("parameters=", NewGame().CanonicalText());
    }

    [Fact]
    public void TEC_008_the_hash_after_a_two_hex_move_is_pinned()
    {
        // Pinned value computed independently in Python from the canonical text format, not from this code.
        var game = NewGame();
        Assert.True(Move(game, 2).Accepted);

        Assert.Equal("36a9de83fc9f4f493df93e3ff346d93ebdaccd07e96f5bfdfc7b5049ccb31d09", game.ComputeHash());
    }
}
