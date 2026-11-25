using System.Reflection;
using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Core.Tests;

public class MoveTests
{
    private static HexMap Map() =>
        MapLoader.Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "maps", "sample.json")));

    private static HexCoord Hex(int col, int row) => HexCoord.FromOffset(new OffsetCoord(col, row));

    private static GameState New(int budget = 10, int cost = 1, (int, int)? p1 = null, (int, int)? p2 = null,
        PlayerId first = PlayerId.Player1)
    {
        var a = p1 ?? (5, 20);
        var b = p2 ?? (24, 30);
        return GameState.NewGame(Map(),
            new GameConfig(first, budget, new OffsetCoord(a.Item1, a.Item2), new OffsetCoord(b.Item1, b.Item2), cost));
    }

    private static MoveCommand Move(PlayerId player, HexCoord target) => new MoveCommand(player, player, target);

    private static GameState Accept(GameState s, GameCommand c)
    {
        var r = s.Apply(c);
        Assert.True(r.IsAccepted, r.RejectionReason);
        return r.State;
    }

    // Walks one token n hexes in a fixed direction (an index into HexCoord.Neighbours), one Move per hex.
    private static GameState Walk(GameState s, PlayerId player, int n, int direction = 0)
    {
        for (var i = 0; i < n; i++)
        {
            s = Accept(s, Move(player, s.TokenOf(player).Position.Neighbours()[direction]));
        }
        return s;
    }

    private static string Snapshot(GameState s) =>
        s.CurrentPlayer + "|" + string.Join(";", s.Tokens.Select(t => t.Owner + "@" + t.Position.Q + "," + t.Position.R + ":" + t.Budget));

    private static void AssertRejected(GameState s, GameCommand c, string? reasonPart = null)
    {
        var before = Snapshot(s);
        var r = s.Apply(c);
        Assert.False(r.IsAccepted);
        Assert.False(string.IsNullOrWhiteSpace(r.RejectionReason));
        if (reasonPart != null) Assert.Contains(reasonPart, r.RejectionReason);
        Assert.Same(s, r.State);
        Assert.Equal(before, Snapshot(r.State));
    }

    [Fact]
    public void DLV_013_a_move_is_a_command_holding_only_plain_data()
    {
        var plain = new HashSet<Type> { typeof(PlayerId), typeof(HexCoord) };
        var members = typeof(MoveCommand).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(p => p.PropertyType).ToList();

        Assert.Equal(2, members.Count); // Player is on the base class
        Assert.All(members, m => Assert.Contains(m, plain));
        Assert.True(typeof(GameCommand).IsAssignableFrom(typeof(MoveCommand)));
    }

    [Fact]
    public void DLV_013_a_move_returns_a_new_state_and_leaves_the_old_one_unchanged()
    {
        var s = New();
        var before = Snapshot(s);

        var next = Accept(s, Move(PlayerId.Player1, s.TokenOf(PlayerId.Player1).Position.Neighbours()[0]));

        Assert.NotSame(s, next);
        Assert.Equal(before, Snapshot(s));
        Assert.NotEqual(before, Snapshot(next));
    }

    [Fact]
    public void SUP_005_move_to_an_adjacent_empty_passable_hex_moves_the_token_and_spends_one_hex_of_budget()
    {
        var s = New();
        var target = s.TokenOf(PlayerId.Player1).Position.Neighbours()[0];

        s = Accept(s, Move(PlayerId.Player1, target));

        Assert.Equal(target, s.TokenOf(PlayerId.Player1).Position);
        Assert.Equal(9, s.TokenOf(PlayerId.Player1).Budget);
        Assert.Equal(10, s.TokenOf(PlayerId.Player2).Budget);
        Assert.Equal(PlayerId.Player1, s.CurrentPlayer);
    }

    [Fact]
    public void SUP_005_ten_single_hex_moves_in_one_turn_all_succeed_and_leave_budget_0()
    {
        var s = Walk(New(), PlayerId.Player1, 10);

        Assert.Equal(0, s.TokenOf(PlayerId.Player1).Budget);
    }

    [Fact]
    public void SUP_005_movement_cost_per_hex_comes_from_the_config()
    {
        var s = Walk(New(cost: 3), PlayerId.Player1, 3);

        Assert.Equal(1, s.TokenOf(PlayerId.Player1).Budget);
    }

    [Fact]
    public void SUP_006_a_move_with_budget_0_is_rejected_and_the_state_is_unchanged()
    {
        var s = Walk(New(), PlayerId.Player1, 10);

        AssertRejected(s, Move(PlayerId.Player1, s.TokenOf(PlayerId.Player1).Position.Neighbours()[0]), "budget");
        Assert.Equal(0, s.TokenOf(PlayerId.Player1).Budget);
    }

    [Fact]
    public void SUP_006_a_move_costing_more_than_the_budget_left_is_rejected_and_budget_never_goes_below_0()
    {
        var s = New(budget: 5, cost: 2);
        s = Walk(s, PlayerId.Player1, 2); // budget 1 left, cost 2

        Assert.Equal(1, s.TokenOf(PlayerId.Player1).Budget);
        AssertRejected(s, Move(PlayerId.Player1, s.TokenOf(PlayerId.Player1).Position.Neighbours()[0]), "budget");
        Assert.Equal(1, s.TokenOf(PlayerId.Player1).Budget);
    }

    [Fact]
    public void TRN_029_the_budget_is_spent_straight_away_after_one_move()
    {
        var s = New();
        var next = s.Apply(Move(PlayerId.Player1, s.TokenOf(PlayerId.Player1).Position.Neighbours()[0]));

        Assert.Equal(9, next.State.TokenOf(PlayerId.Player1).Budget);
        Assert.Equal(PlayerId.Player1, next.State.CurrentPlayer); // the turn has not ended
    }

    [Fact]
    public void TRN_030_movement_can_be_split_across_other_commands_in_one_turn()
    {
        var s = Walk(New(), PlayerId.Player1, 3);
        Assert.Equal(7, s.TokenOf(PlayerId.Player1).Budget);

        // Other commands in between: rejected moves do not change anything.
        AssertRejected(s, Move(PlayerId.Player1, s.TokenOf(PlayerId.Player1).Position)); // not adjacent (own hex)
        AssertRejected(s, Move(PlayerId.Player2, s.TokenOf(PlayerId.Player2).Position.Neighbours()[0])); // not Player 2's turn
        AssertRejected(s, Move(PlayerId.Player1, Hex(-1, -1))); // off the map

        s = Walk(s, PlayerId.Player1, 2);

        Assert.Equal(5, s.TokenOf(PlayerId.Player1).Budget);
    }

    [Fact]
    public void TRN_023_a_move_outside_the_map_is_rejected()
    {
        var s = New(p1: (0, 12));
        var target = Hex(-1, 12);
        Assert.Equal(1, s.TokenOf(PlayerId.Player1).Position.DistanceTo(target));

        AssertRejected(s, Move(PlayerId.Player1, target), "outside");
    }

    [Fact]
    public void TRN_024_a_move_onto_a_mountain_is_rejected()
    {
        var s = New(p1: (19, 10));
        var target = Hex(20, 10);
        Assert.Equal(TerrainType.Mountain, s.Map.TerrainAt(target));
        Assert.Equal(1, s.TokenOf(PlayerId.Player1).Position.DistanceTo(target));

        AssertRejected(s, Move(PlayerId.Player1, target), "Mountain");
    }

    [Fact]
    public void TRN_024_a_move_onto_a_sea_hex_is_rejected()
    {
        var s = New(p1: (4, 5));
        var target = Hex(3, 5);
        Assert.Equal(TerrainType.Sea, s.Map.TerrainAt(target));
        Assert.Equal(1, s.TokenOf(PlayerId.Player1).Position.DistanceTo(target));

        AssertRejected(s, Move(PlayerId.Player1, target), "Sea");
    }

    [Fact]
    public void TRN_024_a_move_onto_a_river_hex_is_rejected()
    {
        var s = New(p1: (13, 20));
        var target = Hex(14, 20);
        Assert.Equal(TerrainType.River, s.Map.TerrainAt(target));
        Assert.Equal(1, s.TokenOf(PlayerId.Player1).Position.DistanceTo(target));

        AssertRejected(s, Move(PlayerId.Player1, target), "River");
    }

    [Fact]
    public void TRN_020_and_TRN_023_a_move_onto_the_other_token_is_rejected()
    {
        var s = New();
        var enemy = s.TokenOf(PlayerId.Player2).Position;

        AssertRejected(s, Move(PlayerId.Player1, enemy), "occupied");
    }

    [Fact]
    public void DEC_087_a_move_next_to_the_enemy_token_is_rejected_as_contact()
    {
        var s = New(p1: (5, 20), p2: (5, 23)); // two hexes between them
        var enemy = s.TokenOf(PlayerId.Player2).Position;
        var target = Hex(5, 21);
        Assert.Equal(2, target.DistanceTo(enemy)); // (5,21) is fine
        s = Accept(s, Move(PlayerId.Player1, target));
        var next = Hex(5, 22);
        Assert.Equal(1, next.DistanceTo(enemy));

        AssertRejected(s, Move(PlayerId.Player1, next), "contact: battles not in spike");
    }

    [Fact]
    public void DEC_087_the_contact_rule_also_applies_to_the_second_player()
    {
        var s = New(p1: (5, 20), p2: (5, 23), first: PlayerId.Player2);

        s = Accept(s, Move(PlayerId.Player2, Hex(5, 22)));
        AssertRejected(s, Move(PlayerId.Player2, Hex(5, 21)), "contact: battles not in spike");
    }

    [Fact]
    public void SUP_005_a_move_to_a_hex_that_is_not_adjacent_is_rejected()
    {
        var s = New();
        var pos = s.TokenOf(PlayerId.Player1).Position;
        var twoAway = pos.Neighbours()[0].Neighbours()[0];
        Assert.Equal(2, pos.DistanceTo(twoAway));

        AssertRejected(s, Move(PlayerId.Player1, twoAway), "not next to");
        AssertRejected(s, Move(PlayerId.Player1, pos), "not next to");
    }

    [Fact]
    public void TRN_007_a_move_for_the_other_players_token_is_rejected()
    {
        var s = New();
        var enemyTarget = s.TokenOf(PlayerId.Player2).Position.Neighbours()[0];

        AssertRejected(s, new MoveCommand(PlayerId.Player1, PlayerId.Player2, enemyTarget), "own token");
    }

    [Fact]
    public void TRN_007_a_move_issued_by_the_player_whose_turn_it_is_not_is_rejected()
    {
        var s = New();
        var target = s.TokenOf(PlayerId.Player2).Position.Neighbours()[0];

        AssertRejected(s, Move(PlayerId.Player2, target), "Player1");
        AssertRejected(s, new MoveCommand(PlayerId.Player2, PlayerId.Player1, s.TokenOf(PlayerId.Player1).Position.Neighbours()[0]));
    }

    [Fact]
    public void TRN_007_a_move_for_an_unknown_token_is_rejected()
    {
        var s = New();

        AssertRejected(s, new MoveCommand(PlayerId.Player1, (PlayerId)9, Hex(5, 21)), "Unknown token");
    }

    [Fact]
    public void SUP_005_the_second_player_moves_their_own_token_in_their_turn()
    {
        var s = New();
        s = Accept(s, new EndTurnCommand(PlayerId.Player1));

        s = Walk(s, PlayerId.Player2, 4, 1);

        Assert.Equal(6, s.TokenOf(PlayerId.Player2).Budget);
        Assert.Equal(10, s.TokenOf(PlayerId.Player1).Budget);
    }

    [Fact]
    public void TEC_008_the_same_commands_always_give_the_same_state()
    {
        GameState Play()
        {
            var s = Walk(New(), PlayerId.Player1, 4);
            s = Accept(s, new EndTurnCommand(PlayerId.Player1));
            s = Walk(s, PlayerId.Player2, 3, 2);
            return Accept(s, new EndTurnCommand(PlayerId.Player2));
        }

        Assert.Equal(Snapshot(Play()), Snapshot(Play()));
    }

    [Fact]
    public void SUP_005_movement_cost_per_hex_is_read_from_the_scenario_file()
    {
        var withCost = GameConfigLoader.Load("{\"startingBudget\":10,\"movementCostPerHex\":2,\"tokens\":{\"1\":[1,2],\"2\":[3,4]}}");
        var sample = GameConfigLoader.Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "maps", "sample-scenario.json")));

        Assert.Equal(2, withCost.MovementCostPerHex);
        Assert.Equal(1, sample.MovementCostPerHex);
    }

    [Fact]
    public void SUP_005_a_scenario_file_without_a_movement_cost_is_rejected_there_is_no_default()
    {
        var e = Assert.Throws<SetupException>(() =>
            GameConfigLoader.Load("{\"startingBudget\":10,\"tokens\":{\"1\":[1,2],\"2\":[3,4]}}"));

        Assert.Contains("movementCostPerHex", e.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("\"2\"")]
    [InlineData("1.5")]
    [InlineData("10001")]
    public void SUP_005_a_bad_movement_cost_in_the_scenario_file_is_rejected(string value)
    {
        Assert.Throws<SetupException>(() =>
            GameConfigLoader.Load("{\"startingBudget\":10,\"movementCostPerHex\":" + value + ",\"tokens\":{\"1\":[1,2],\"2\":[3,4]}}"));
    }

    [Fact]
    public void SUP_005_a_game_cannot_start_with_a_movement_cost_below_1()
    {
        Assert.Throws<SetupException>(() => New(cost: 0));
    }
}
