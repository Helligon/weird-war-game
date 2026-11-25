using System.Reflection;
using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Core.Tests;

public class TurnTests
{
    private static string MapsFile(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "maps", name));

    private static HexMap Map() => MapLoader.Load(MapsFile("sample.json"));

    private static GameConfig Config(PlayerId first = PlayerId.Player1, int budget = 10,
        (int, int)? p1 = null, (int, int)? p2 = null)
    {
        var a = p1 ?? (5, 20);
        var b = p2 ?? (24, 30);
        return new GameConfig(first, budget, new OffsetCoord(a.Item1, a.Item2), new OffsetCoord(b.Item1, b.Item2));
    }

    private static GameState New(GameConfig? config = null) => GameState.NewGame(Map(), config ?? Config());

    private static GameState Accept(GameState s, GameCommand c)
    {
        var r = s.Apply(c);
        Assert.True(r.IsAccepted, r.RejectionReason);
        return r.State;
    }

    private static string Snapshot(GameState s) =>
        s.CurrentPlayer + "|" + string.Join(";", s.Tokens.Select(t => t.Owner + "@" + t.Position.Q + "," + t.Position.R + ":" + t.Budget));

    [Fact]
    public void DLV_013_state_exposes_no_public_setters_or_mutating_methods()
    {
        var type = typeof(GameState);

        Assert.All(type.GetProperties(), p =>
            Assert.True(p.SetMethod == null || !p.SetMethod.IsPublic, p.Name + " has a public setter"));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
        var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName).Select(m => m.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "Apply", "TokenOf" }, publicMethods);
    }

    [Fact]
    public void DLV_013_every_type_reachable_from_the_state_is_immutable()
    {
        var types = new[]
        {
            typeof(GameState), typeof(Token), typeof(GameConfig), typeof(HexMap),
            typeof(CommandResult), typeof(HexCoord), typeof(OffsetCoord),
        };
        foreach (var type in types)
        {
            Assert.All(type.GetProperties(BindingFlags.Public | BindingFlags.Instance), p =>
                Assert.True(p.SetMethod == null || !p.SetMethod.IsPublic, type.Name + "." + p.Name + " has a public setter"));
            Assert.All(type.GetFields(BindingFlags.Public | BindingFlags.Instance), f =>
                Assert.True(f.IsInitOnly, type.Name + "." + f.Name + " is a public mutable field"));
        }
    }

    [Fact]
    public void DLV_013_tokens_list_cannot_be_used_to_change_the_state()
    {
        var s = New();
        var before = Snapshot(s);

        Assert.False(s.Tokens is Token[]);
        Assert.Throws<NotSupportedException>(() => ((IList<Token>)s.Tokens)[0] = new Token(PlayerId.Player1, new HexCoord(0, 0), 1));
        Assert.Equal(before, Snapshot(s));
    }

    [Fact]
    public void DLV_013_applying_a_command_returns_a_new_state_and_leaves_the_old_one_unchanged()
    {
        var s = New();
        var before = Snapshot(s);

        var next = Accept(s, new EndTurnCommand(PlayerId.Player1));

        Assert.NotSame(s, next);
        Assert.Equal(before, Snapshot(s));
        Assert.Equal(PlayerId.Player2, next.CurrentPlayer);
    }

    [Fact]
    public void DLV_013_a_rejection_carries_a_reason_and_the_same_state()
    {
        var s = New();

        var r = s.Apply(new EndTurnCommand(PlayerId.Player2));

        Assert.False(r.IsAccepted);
        Assert.False(string.IsNullOrWhiteSpace(r.RejectionReason));
        Assert.Same(s, r.State);
    }

    [Fact]
    public void TEC_008_commands_hold_only_plain_data()
    {
        var plain = new HashSet<Type> { typeof(PlayerId), typeof(int), typeof(string), typeof(bool), typeof(HexCoord), typeof(OffsetCoord) };
        var commandTypes = typeof(GameCommand).Assembly.GetTypes().Where(t => typeof(GameCommand).IsAssignableFrom(t)).ToList();

        Assert.Contains(typeof(EndTurnCommand), commandTypes);
        foreach (var t in commandTypes)
        {
            var members = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(f => f.FieldType)
                .Concat(t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(p => p.PropertyType));
            Assert.All(members, m => Assert.True(plain.Contains(m), t.Name + " holds a non-plain type " + m.Name));
        }
        Assert.Equal(PlayerId.Player2, new EndTurnCommand(PlayerId.Player2).Player);
    }

    [Fact]
    public void TRN_006_state_does_not_change_when_no_command_is_applied()
    {
        var s = New();
        var before = Snapshot(s);

        Thread.Sleep(50);

        Assert.Equal(before, Snapshot(s));
        Assert.Equal(PlayerId.Player1, s.CurrentPlayer);
    }

    [Fact]
    public void TRN_007_end_turn_alternates_between_the_two_players()
    {
        var s = New();
        Assert.Equal(PlayerId.Player1, s.CurrentPlayer);

        s = Accept(s, new EndTurnCommand(PlayerId.Player1));
        Assert.Equal(PlayerId.Player2, s.CurrentPlayer);

        s = Accept(s, new EndTurnCommand(PlayerId.Player2));
        Assert.Equal(PlayerId.Player1, s.CurrentPlayer);
    }

    [Fact]
    public void TRN_007_end_turn_from_the_player_whose_turn_it_is_not_is_rejected_with_a_reason_and_no_change()
    {
        var s = New();
        var before = Snapshot(s);

        var r = s.Apply(new EndTurnCommand(PlayerId.Player2));

        Assert.False(r.IsAccepted);
        Assert.Contains("Player1", r.RejectionReason);
        Assert.Equal(before, Snapshot(r.State));
        Assert.Equal(before, Snapshot(s));
    }

    [Fact]
    public void TRN_007_unknown_player_and_null_command_are_rejected()
    {
        var s = New();

        Assert.False(s.Apply(new EndTurnCommand((PlayerId)7)).IsAccepted);
        Assert.False(s.Apply(null!).IsAccepted);
    }

    [Fact]
    public void TRN_033_stand_in_first_player_comes_from_config()
    {
        Assert.Equal(PlayerId.Player2, New(Config(first: PlayerId.Player2)).CurrentPlayer);
        Assert.Equal(PlayerId.Player1, New(Config(first: PlayerId.Player1)).CurrentPlayer);
    }

    [Fact]
    public void TRN_033_stand_in_the_second_player_can_act_first_and_the_other_cannot()
    {
        var s = New(Config(first: PlayerId.Player2));

        Assert.False(s.Apply(new EndTurnCommand(PlayerId.Player1)).IsAccepted);
        Assert.Equal(PlayerId.Player1, Accept(s, new EndTurnCommand(PlayerId.Player2)).CurrentPlayer);
    }

    [Fact]
    public void DEC_087_budget_refills_only_the_token_of_the_player_whose_turn_starts()
    {
        var s = New();
        // Test-only: build a state where both tokens are partly spent via the internal handler.
        s = s.WithToken(new Token(PlayerId.Player1, s.TokenOf(PlayerId.Player1).Position, 3))
             .WithToken(new Token(PlayerId.Player2, s.TokenOf(PlayerId.Player2).Position, 4));

        s = Accept(s, new EndTurnCommand(PlayerId.Player1)); // Player 2's turn starts
        Assert.Equal(3, s.TokenOf(PlayerId.Player1).Budget);
        Assert.Equal(10, s.TokenOf(PlayerId.Player2).Budget);

        s = s.WithToken(new Token(PlayerId.Player2, s.TokenOf(PlayerId.Player2).Position, 6));
        s = Accept(s, new EndTurnCommand(PlayerId.Player2)); // Player 1's turn starts
        Assert.Equal(10, s.TokenOf(PlayerId.Player1).Budget);
        Assert.Equal(6, s.TokenOf(PlayerId.Player2).Budget);
    }

    [Fact]
    public void DEC_087_starting_budget_is_configurable()
    {
        var s = New(Config(budget: 4));
        Assert.Equal(4, s.TokenOf(PlayerId.Player1).Budget);
        Assert.Equal(4, s.TokenOf(PlayerId.Player2).Budget);
    }

    [Fact]
    public void DEC_087_new_game_puts_both_tokens_on_their_start_hexes_with_full_budget()
    {
        var s = New();

        Assert.Equal(HexCoord.FromOffset(new OffsetCoord(5, 20)), s.TokenOf(PlayerId.Player1).Position);
        Assert.Equal(HexCoord.FromOffset(new OffsetCoord(24, 30)), s.TokenOf(PlayerId.Player2).Position);
        Assert.All(s.Tokens, t => Assert.Equal(10, t.Budget));
        Assert.Equal(PlayerId.Player1, s.TokenOf(PlayerId.Player1).Owner);
        Assert.Equal(PlayerId.Player2, s.TokenOf(PlayerId.Player2).Owner);
    }

    [Theory]
    [InlineData(0, 0, 24, 30)]    // Player 1 on sea
    [InlineData(5, 20, 20, 10)]   // Player 2 on mountain
    [InlineData(14, 5, 24, 30)]   // Player 1 on river
    [InlineData(5, 20, 5, 20)]    // same hex
    [InlineData(5, 20, 6, 20)]    // adjacent
    [InlineData(5, 20, 5, 21)]    // adjacent
    [InlineData(-1, 0, 24, 30)]   // off the map
    [InlineData(5, 20, 30, 5)]    // off the map
    public void DEC_087_setup_rejects_impassable_shared_adjacent_or_off_map_starts(int c1, int r1, int c2, int r2)
    {
        Assert.Throws<SetupException>(() => New(Config(p1: (c1, r1), p2: (c2, r2))));
    }

    [Fact]
    public void DEC_087_setup_accepts_tokens_two_hexes_apart()
    {
        var s = New(Config(p1: (5, 20), p2: (5, 22)));
        Assert.Equal(2, s.TokenOf(PlayerId.Player1).Position.DistanceTo(s.TokenOf(PlayerId.Player2).Position));
    }

    [Fact]
    public void DEC_087_setup_rejects_a_bad_first_player_or_budget()
    {
        Assert.Throws<SetupException>(() => New(Config(first: (PlayerId)0)));
        Assert.Throws<SetupException>(() => New(Config(budget: 0)));
    }

    [Fact]
    public void DEC_087_sample_scenario_file_loads_and_starts_a_game()
    {
        var config = GameConfigLoader.Load(MapsFile("sample-scenario.json"));

        Assert.Equal(PlayerId.Player1, config.FirstPlayer);
        Assert.Equal(10, config.StartingBudget);
        var s = GameState.NewGame(Map(), config);
        Assert.Equal(PlayerId.Player1, s.CurrentPlayer);
    }

    [Fact]
    public void DEC_087_scenario_file_sets_first_player_budget_and_positions()
    {
        var config = GameConfigLoader.Load("{\"firstPlayer\":2,\"startingBudget\":7,\"tokens\":{\"1\":[1,2],\"2\":[3,4]}}");

        Assert.Equal(PlayerId.Player2, config.FirstPlayer);
        Assert.Equal(7, config.StartingBudget);
        Assert.Equal(new OffsetCoord(1, 2), config.Player1Start);
        Assert.Equal(new OffsetCoord(3, 4), config.Player2Start);
    }

    [Fact]
    public void TRN_033_stand_in_first_player_defaults_to_player_1_when_missing()
    {
        var config = GameConfigLoader.Load("{\"startingBudget\":10,\"tokens\":{\"1\":[1,2],\"2\":[3,4]}}");

        Assert.Equal(PlayerId.Player1, config.FirstPlayer);
    }

    [Fact]
    public void TRN_007_unknown_player_id_is_not_treated_as_player_2()
    {
        var s = New();

        Assert.Throws<ArgumentOutOfRangeException>(() => s.TokenOf((PlayerId)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.TokenOf((PlayerId)7));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.WithToken(new Token((PlayerId)3, new HexCoord(0, 0), 1)));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("not json")]
    [InlineData("{\"firstPlayer\":3,\"startingBudget\":10,\"tokens\":{\"1\":[1,2],\"2\":[3,4]}}")]
    [InlineData("{\"firstPlayer\":1,\"startingBudget\":0,\"tokens\":{\"1\":[1,2],\"2\":[3,4]}}")]
    [InlineData("{\"firstPlayer\":1,\"startingBudget\":10}")]
    [InlineData("{\"firstPlayer\":1,\"startingBudget\":10,\"tokens\":{\"1\":[1,2]}}")]
    [InlineData("{\"firstPlayer\":1,\"startingBudget\":10,\"tokens\":{\"1\":[1,2],\"2\":[3]}}")]
    public void DEC_087_malformed_scenario_files_are_rejected(string json)
    {
        Assert.Throws<SetupException>(() => GameConfigLoader.Load(json));
    }
}
