using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Core.Tests;

public class DeterminismTests
{
    // Pinned on macOS and checked on Linux CI (TEC-008). If it changes, the canonical format or the rules changed:
    // update it on purpose, never to make a failing run pass.
    private const string GoldenHash = "428af84e3ada6e71215cbe7246739497ecd6e5e3a5db388047156f41f63848d4";

    private static string MapsFile(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "maps", name));

    private static GameState Fresh() =>
        GameState.NewGame(MapLoader.Load(MapsFile("sample.json")), GameConfigLoader.Load(MapsFile("sample-scenario.json")));

    private static readonly PlayerId P1 = PlayerId.Player1;
    private static readonly PlayerId P2 = PlayerId.Player2;

    private static MoveCommand Step(PlayerId p, GameState s, int dir) =>
        new MoveCommand(p, p, s.TokenOf(p).Position.Neighbours()[dir]);

    private static MoveCommand MoveTo(PlayerId p, int col, int row) =>
        new MoveCommand(p, p, HexCoord.FromOffset(new OffsetCoord(col, row)));

    // 30 commands over 4 turns: valid moves, rejected moves (wrong turn, not adjacent, enemy hex, off map,
    // out of budget) and end turns. Targets are fixed hexes, so the list is plain data.
    private static List<GameCommand> CommandList()
    {
        var s = Fresh();
        var list = new List<GameCommand>();
        void Add(GameCommand c)
        {
            list.Add(c);
            s = s.Apply(c).State;
        }
        // Turn 1, Player 1: three steps up, then bad moves.
        Add(Step(P1, s, 0)); Add(Step(P1, s, 0)); Add(Step(P1, s, 1));
        Add(new MoveCommand(P2, P2, new HexCoord(0, 0)));            // not Player 2's turn
        Add(MoveTo(P1, 20, 20));                                      // not adjacent
        Add(MoveTo(P1, -1, 5));                                       // off the map
        Add(new MoveCommand(P1, P2, new HexCoord(1, 1)));            // another player's token
        Add(new EndTurnCommand(P2));                                  // not Player 2's turn
        Add(new EndTurnCommand(P1));
        // Turn 2, Player 2.
        Add(Step(P2, s, 3)); Add(Step(P2, s, 3)); Add(Step(P2, s, 4));
        Add(MoveTo(P2, 5, 5));                                        // not adjacent
        Add(new EndTurnCommand(P2));
        // Turn 3, Player 1: spend the whole budget (10), then one more is rejected.
        for (var i = 0; i < 4; i++) Add(Step(P1, s, i % 2 == 0 ? 0 : 1));
        Add(Step(P1, s, 3)); Add(Step(P1, s, 4)); Add(Step(P1, s, 3));
        Add(Step(P1, s, 4)); Add(Step(P1, s, 3)); Add(Step(P1, s, 4));
        Add(Step(P1, s, 0));                                          // out of budget
        Add(new EndTurnCommand(P1));
        // Turn 4, Player 2.
        Add(Step(P2, s, 0)); Add(Step(P2, s, 1));
        Add(new EndTurnCommand(P2));
        return list;
    }

    private static GameState Replay(GameState start, IEnumerable<GameCommand> commands, out int accepted, out int rejected)
    {
        accepted = 0;
        rejected = 0;
        var s = start;
        foreach (var c in commands)
        {
            var r = s.Apply(c);
            if (r.IsAccepted) accepted++; else rejected++;
            s = r.State;
        }
        return s;
    }

    private static GameState Replay(IEnumerable<GameCommand> commands) => Replay(Fresh(), commands, out _, out _);

    [Fact]
    public void TEC_005_the_test_list_has_20_plus_commands_with_valid_and_rejected_moves_and_end_turns()
    {
        var list = CommandList();

        Replay(Fresh(), list, out var accepted, out var rejected);

        Assert.True(list.Count >= 20);
        Assert.True(accepted >= 10, "accepted " + accepted);
        Assert.True(rejected >= 5, "rejected " + rejected);
        Assert.True(list.OfType<EndTurnCommand>().Count() >= 3);
        Assert.True(list.OfType<MoveCommand>().Count() >= 10);
    }

    [Fact]
    public void TEC_005_same_commands_on_two_fresh_states_give_identical_final_states()
    {
        var list = CommandList();

        var a = Replay(list);
        var b = Replay(list);

        Assert.Equal(CanonicalState.Serialise(a), CanonicalState.Serialise(b));
        Assert.Equal(CanonicalState.Hash(a), CanonicalState.Hash(b));
        Assert.Equal(a.CurrentPlayer, b.CurrentPlayer);
    }

    [Fact]
    public void TEC_005_final_state_matches_the_golden_hash()
    {
        var hash = CanonicalState.Hash(Replay(CommandList()));

        Assert.Equal(GoldenHash, hash);
    }

    [Fact]
    public void TEC_005_golden_hash_is_the_same_on_repeated_runs()
    {
        var first = CanonicalState.Hash(Replay(CommandList()));
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(first, CanonicalState.Hash(Replay(CommandList())));
        }
    }

    [Fact]
    public void TEC_005_command_list_round_trips_through_json_and_replays_to_the_same_state()
    {
        var list = CommandList();

        var json = CommandSerialiser.SerialiseList(list);
        var loaded = CommandSerialiser.DeserialiseList(json);

        Assert.Equal(list.Count, loaded.Count);
        Assert.Equal(json, CommandSerialiser.SerialiseList(loaded));
        Assert.Equal(CanonicalState.Hash(Replay(list)), CanonicalState.Hash(Replay(loaded)));
        Assert.Equal(GoldenHash, CanonicalState.Hash(Replay(loaded)));
    }

    [Fact]
    public void TEC_008_a_serialised_command_holds_only_plain_data()
    {
        var move = CommandSerialiser.Serialise(new MoveCommand(P1, P1, new HexCoord(3, -4)));
        var end = CommandSerialiser.Serialise(new EndTurnCommand(P2));

        Assert.Equal("{\"type\":\"move\",\"player\":1,\"tokenOwner\":1,\"q\":3,\"r\":-4}", move);
        Assert.Equal("{\"type\":\"endTurn\",\"player\":2}", end);
        // Only strings (type names, keys) and whole numbers: no objects other than the command, no state, no type names of .NET.
        foreach (var json in new[] { move, end })
        {
            Assert.Matches("^\\{(\"[A-Za-z]+\":(\"[A-Za-z]+\"|-?[0-9]+),?)+\\}$", json);
            Assert.DoesNotContain("WeirdWar", json);
        }
    }

    [Fact]
    public void TEC_008_command_json_is_the_same_under_any_culture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            var expected = CommandSerialiser.SerialiseList(CommandList());
            var expectedHash = CanonicalState.Hash(Replay(CommandList()));
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("ar-SA");

            Assert.Equal(expected, CommandSerialiser.SerialiseList(CommandList()));
            Assert.Equal(expectedHash, CanonicalState.Hash(Replay(CommandList())));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void TEC_005_different_command_lists_give_different_hashes()
    {
        var list = CommandList();
        var shorter = list.Take(list.Count - 1).ToList();
        var other = new List<GameCommand> { new EndTurnCommand(P1) };

        var hashes = new[] { Replay(list), Replay(shorter), Replay(other), Fresh() }.Select(CanonicalState.Hash).ToList();

        Assert.Equal(hashes.Count, hashes.Distinct().Count());
    }

    [Fact]
    public void TEC_005_a_rejected_command_does_not_change_the_hash()
    {
        var s = Fresh();
        var before = CanonicalState.Hash(s);

        var r = s.Apply(new EndTurnCommand(P2));

        Assert.False(r.IsAccepted);
        Assert.Equal(before, CanonicalState.Hash(r.State));
    }

    [Fact]
    public void TEC_005_hash_is_64_lower_case_hex_characters_and_does_not_use_GetHashCode()
    {
        var hash = CanonicalState.Hash(Fresh());

        Assert.Matches("^[0-9a-f]{64}$", hash);
        var text = CanonicalState.Serialise(Fresh());
        Assert.StartsWith("{\"currentPlayer\":1,\"config\":{", text);
    }

    [Fact]
    public void TEC_008_malformed_saved_commands_are_rejected_with_a_clear_error()
    {
        Assert.Throws<CommandFormatException>(() => CommandSerialiser.Deserialise("{\"type\":\"teleport\",\"player\":1}"));
        Assert.Throws<CommandFormatException>(() => CommandSerialiser.Deserialise("{\"type\":\"endTurn\",\"player\":3}"));
        Assert.Throws<CommandFormatException>(() => CommandSerialiser.Deserialise("{\"type\":\"move\",\"player\":1,\"tokenOwner\":1,\"q\":1}"));
        Assert.Throws<CommandFormatException>(() => CommandSerialiser.Deserialise("[1]"));
        Assert.Throws<CommandFormatException>(() => CommandSerialiser.DeserialiseList("{}"));
        Assert.Throws<CommandFormatException>(() => CommandSerialiser.DeserialiseList("[{"));
    }

    [Fact]
    public void DLV_013_every_command_type_can_be_saved_and_loaded()
    {
        // A new GameCommand subclass must be added to CommandSerialiser, or this fails.
        var samples = new GameCommand[]
        {
            new MoveCommand(P2, P2, new HexCoord(-2, 7)),
            new EndTurnCommand(P1),
        };
        var commandTypes = typeof(GameCommand).Assembly.GetTypes()
            .Where(t => typeof(GameCommand).IsAssignableFrom(t) && !t.IsAbstract).ToList();

        Assert.Equal(commandTypes.OrderBy(t => t.Name), samples.Select(c => c.GetType()).OrderBy(t => t.Name));
        foreach (var c in samples)
        {
            var back = CommandSerialiser.Deserialise(CommandSerialiser.Serialise(c));
            Assert.Equal(c.GetType(), back.GetType());
            Assert.Equal(CommandSerialiser.Serialise(c), CommandSerialiser.Serialise(back));
        }
    }
}
