using System.Reflection;
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

    // The pinned command list: fixtures/determinism-commands.json, 30 commands over 4 turns, saved as plain data so
    // the test does not run the engine to build its own input. It holds valid moves, rejected moves (wrong turn,
    // not adjacent, off the map, another player's token, out of budget), rejected end turns and accepted end turns.
    private static List<GameCommand> CommandList() =>
        CommandSerialiser.DeserialiseList(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "determinism-commands.json")));

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
    public void TEC_005_the_test_list_has_20_plus_commands_covering_several_turns_and_several_rejection_reasons()
    {
        var list = CommandList();
        var s = Fresh();
        var acceptedEndTurns = 0;
        var acceptedMoves = 0;
        var turnChanges = 0;
        var reasons = new HashSet<string>();
        foreach (var c in list)
        {
            var before = s.CurrentPlayer;
            var r = s.Apply(c);
            if (r.IsAccepted)
            {
                if (c is EndTurnCommand) acceptedEndTurns++;
                if (c is MoveCommand) acceptedMoves++;
                if (r.State.CurrentPlayer != before) turnChanges++;
            }
            else
            {
                reasons.Add(r.RejectionReason!);
            }
            s = r.State;
        }

        Assert.True(list.Count >= 20);
        Assert.True(acceptedMoves >= 10, "accepted moves " + acceptedMoves);
        Assert.True(acceptedEndTurns >= 3, "accepted end turns " + acceptedEndTurns);
        Assert.Equal(acceptedEndTurns, turnChanges);
        // wrong turn, not adjacent, outside the map, another player's token, out of budget
        Assert.True(reasons.Count >= 5, "rejection reasons: " + string.Join(" | ", reasons));
        Assert.Contains(reasons, x => x.Contains("turn"));
        Assert.Contains(reasons, x => x.Contains("budget"));
        Assert.Contains(reasons, x => x.Contains("outside the map"));
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

    // Negative numbers are where culture-dependent formatting shows (ar-SA and de-DE use other minus signs or
    // digits), so this state has negative axial coordinates: col 24, row 0 is r = -12.
    private static GameState NegativeCoordinateState() =>
        GameState.NewGame(MapLoader.Load(MapsFile("sample.json")),
            new GameConfig(P1, 10, new OffsetCoord(24, 0), new OffsetCoord(5, 20), 1));

    [Theory]
    [InlineData("ar-SA")]
    [InlineData("de-DE")]
    [InlineData("sv-SE")]
    public void TEC_008_state_text_hash_and_commands_do_not_depend_on_the_culture(string culture)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var negative = NegativeCoordinateState();
            Assert.Equal(-12, negative.TokenOf(P1).Position.R);
            var expectedText = CanonicalState.Serialise(negative);
            var expectedHash = CanonicalState.Hash(negative);
            var command = new MoveCommand(P1, P1, new HexCoord(-3, -4));
            var expectedCommand = CommandSerialiser.Serialise(command);
            var expectedList = CommandSerialiser.SerialiseList(CommandList());
            var expectedFinal = CanonicalState.Hash(Replay(CommandList()));
            Assert.Contains("\"r\":-12", expectedText);
            Assert.Contains("\"q\":-3,\"r\":-4", expectedCommand);

            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo(culture);

            Assert.Equal(expectedText, CanonicalState.Serialise(NegativeCoordinateState()));
            Assert.Equal(expectedHash, CanonicalState.Hash(NegativeCoordinateState()));
            Assert.Equal(expectedCommand, CommandSerialiser.Serialise(command));
            Assert.Equal(expectedList, CommandSerialiser.SerialiseList(CommandList()));
            Assert.Equal(expectedFinal, CanonicalState.Hash(Replay(CommandList())));
            Assert.Equal(command.Target, ((MoveCommand)CommandSerialiser.Deserialise(expectedCommand)).Target);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    private static string Text(GameState s) => CanonicalState.Serialise(s);

    private static GameState WithConfig(GameConfig c) => GameState.NewGame(MapLoader.Load(MapsFile("sample.json")), c);

    private static GameState Build(HexMap map, GameConfig config, PlayerId current, Token[] tokens) =>
        (GameState)typeof(GameState).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single()
            .Invoke(new object[] { map, config, current, tokens });

    [Fact]
    public void TEC_005_changing_any_one_state_field_changes_the_canonical_text_and_hash()
    {
        var map = MapLoader.Load(MapsFile("sample.json"));
        var c = GameConfigLoader.Load(MapsFile("sample-scenario.json"));
        var baseState = GameState.NewGame(map, c);
        var t1 = baseState.TokenOf(P1);
        var t2 = baseState.TokenOf(P2);
        var cells = new TerrainType[map.Width * map.Height];
        for (var i = 0; i < cells.Length; i++)
            cells[i] = map.TerrainAt(HexCoord.FromOffset(new OffsetCoord(i % map.Width, i / map.Width)));
        var changedCells = (TerrainType[])cells.Clone();
        changedCells[0] = changedCells[0] == TerrainType.Plain ? TerrainType.Sea : TerrainType.Plain;
        var lastCells = (TerrainType[])cells.Clone();
        lastCells[^1] = lastCells[^1] == TerrainType.Plain ? TerrainType.Sea : TerrainType.Plain;

        var variants = new Dictionary<string, GameState>
        {
            ["current player"] = Build(map, c, P2, new[] { t1, t2 }),
            ["first player"] = WithConfig(new GameConfig(P2, c.StartingBudget, c.Player1Start, c.Player2Start, c.MovementCostPerHex)),
            ["starting budget"] = WithConfig(new GameConfig(c.FirstPlayer, c.StartingBudget + 1, c.Player1Start, c.Player2Start, c.MovementCostPerHex)),
            ["movement cost"] = WithConfig(new GameConfig(c.FirstPlayer, c.StartingBudget, c.Player1Start, c.Player2Start, c.MovementCostPerHex + 1)),
            ["player 1 start"] = Build(map, new GameConfig(c.FirstPlayer, c.StartingBudget, new OffsetCoord(c.Player1Start.Column, c.Player1Start.Row + 1), c.Player2Start, c.MovementCostPerHex), P1, new[] { t1, t2 }),
            ["player 2 start"] = Build(map, new GameConfig(c.FirstPlayer, c.StartingBudget, c.Player1Start, new OffsetCoord(c.Player2Start.Column, c.Player2Start.Row + 1), c.MovementCostPerHex), P1, new[] { t1, t2 }),
            ["first map cell"] = Build(new HexMap(map.Width, map.Height, changedCells), c, P1, new[] { t1, t2 }),
            ["last map cell"] = Build(new HexMap(map.Width, map.Height, lastCells), c, P1, new[] { t1, t2 }),
            ["map width"] = Build(new HexMap(map.Width + 1, map.Height, cells.Concat(new TerrainType[map.Height]).ToArray()), c, P1, new[] { t1, t2 }),
            ["token 1 owner"] = Build(map, c, P1, new[] { new Token(P2, t1.Position, t1.Budget), t2 }),
            ["token 2 owner"] = Build(map, c, P1, new[] { t1, new Token(P1, t2.Position, t2.Budget) }),
            ["token 1 q"] = baseState.WithToken(new Token(P1, new HexCoord(t1.Position.Q + 1, t1.Position.R), t1.Budget)),
            ["token 1 r"] = baseState.WithToken(new Token(P1, new HexCoord(t1.Position.Q, t1.Position.R + 1), t1.Budget)),
            ["token 1 budget"] = baseState.WithToken(new Token(P1, t1.Position, t1.Budget - 1)),
            ["token 2 q"] = baseState.WithToken(new Token(P2, new HexCoord(t2.Position.Q + 1, t2.Position.R), t2.Budget)),
            ["token 2 r"] = baseState.WithToken(new Token(P2, new HexCoord(t2.Position.Q, t2.Position.R + 1), t2.Budget)),
            ["token 2 budget"] = baseState.WithToken(new Token(P2, t2.Position, t2.Budget - 1)),
        };

        var baseText = Text(baseState);
        var baseHash = CanonicalState.Hash(baseState);
        foreach (var kv in variants)
        {
            Assert.NotEqual(baseText, Text(kv.Value));
            Assert.NotEqual(baseHash, CanonicalState.Hash(kv.Value));
        }
        // And no two single-field changes collide with each other.
        Assert.Equal(variants.Count, variants.Values.Select(CanonicalState.Hash).Distinct().Count());
    }

    [Fact]
    public void TEC_008_terrain_values_that_do_not_fit_one_digit_are_refused()
    {
        var map = MapLoader.Load(MapsFile("sample.json"));
        var cells = new TerrainType[map.Width * map.Height];
        cells[0] = (TerrainType)10;
        var c = GameConfigLoader.Load(MapsFile("sample-scenario.json"));
        var state = Build(new HexMap(map.Width, map.Height, cells), c, P1,
            new[] { new Token(P1, HexCoord.FromOffset(c.Player1Start), 1), new Token(P2, HexCoord.FromOffset(c.Player2Start), 1) });

        Assert.Throws<InvalidOperationException>(() => CanonicalState.Serialise(state));
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
    public void TEC_005_hash_is_64_lower_case_hex_characters_and_text_starts_with_the_current_player()
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
