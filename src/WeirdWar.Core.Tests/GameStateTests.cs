using WeirdWar.Core.Commands;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class GameStateTests
{
    private static readonly ICommand[] Sequence =
    {
        new AddToTallyCommand(5), new RollIntoTallyCommand(6), new AddToTallyCommand(-2), new RollIntoTallyCommand(20),
    };

    private static GameState Run(ulong seed, IEnumerable<ICommand> commands)
    {
        var state = new GameState(seed);
        foreach (var c in commands) Assert.True(state.ApplyCommand(c).Accepted);
        return state;
    }

    [Fact]
    public void DLV_013_accepted_command_changes_state()
    {
        var state = new GameState(1);

        var result = state.ApplyCommand(new AddToTallyCommand(3));

        Assert.True(result.Accepted);
        Assert.Null(result.Reason);
        Assert.Equal(3, state.Tally);
    }

    [Fact]
    public void DLV_013_rejected_command_leaves_state_and_hash_unchanged()
    {
        var state = Run(1, new ICommand[] { new AddToTallyCommand(1), new RollIntoTallyCommand(6) });
        var hash = state.ComputeHash();
        var text = state.CanonicalText();

        var r1 = state.ApplyCommand(new AddToTallyCommand(-1000));
        var r2 = state.ApplyCommand(new RollIntoTallyCommand(1));

        Assert.False(r1.Accepted);
        Assert.False(r2.Accepted);
        Assert.False(string.IsNullOrEmpty(r1.Reason));
        Assert.Equal(hash, state.ComputeHash());
        Assert.Equal(text, state.CanonicalText());
    }

    [Fact]
    public void DLV_013_rejected_roll_does_not_advance_the_random_source()
    {
        var state = new GameState(9);
        var before = state.Random.Position;

        state.ApplyCommand(new RollIntoTallyCommand(0));

        Assert.Equal(before, state.Random.Position);
    }

    [Fact]
    public void DLV_013_log_holds_accepted_commands_in_order_and_not_rejected_ones()
    {
        var state = new GameState(1);
        var a = new AddToTallyCommand(2);
        var rejected = new AddToTallyCommand(-99);
        var b = new RollIntoTallyCommand(6);

        state.ApplyCommand(a);
        state.ApplyCommand(rejected);
        state.ApplyCommand(b);

        Assert.Equal(2, state.Log.Count);
        Assert.Same(a, state.Log.Entries[0].Command);
        Assert.Same(b, state.Log.Entries[1].Command);
        Assert.Equal(new[] { 0, 1 }, state.Log.Entries.Select(e => e.Step));
    }

    [Fact]
    public void TEC_008_command_serialises_and_reads_back_equal()
    {
        var registry = CommandRegistry.CreateDefault();
        foreach (var c in Sequence)
        {
            var back = registry.ReadCommand(registry.WriteCommand(c));

            Assert.Equal(c, back);
            Assert.Equal(c.GetType(), back.GetType());
        }
    }

    [Fact]
    public void TEC_008_command_json_is_canonical_text()
    {
        var registry = CommandRegistry.CreateDefault();

        Assert.Equal("{\"amount\":5,\"type\":\"addToTally\"}", registry.WriteCommand(new AddToTallyCommand(5)));
    }

    [Theory]
    [InlineData("{\"type\":\"nope\"}")]
    [InlineData("{\"amount\":1}")]
    [InlineData("{\"type\":\"addToTally\"}")]
    [InlineData("{\"type\":\"addToTally\",\"amount\":1.5}")]
    [InlineData("{\"type\":\"rollIntoTally\",\"sides\":99999999999}")]
    [InlineData("[]")]
    public void TEC_008_reading_a_bad_command_throws(string json)
    {
        Assert.Throws<JsonException>(() => CommandRegistry.CreateDefault().ReadCommand(json));
    }

    [Fact]
    public void TEC_008_log_serialises_and_reads_back_equal()
    {
        var registry = CommandRegistry.CreateDefault();
        var state = Run(3, Sequence);

        var back = registry.ReadLog(registry.WriteLog(3, state.Log));

        Assert.Equal(3UL, back.Seed);
        Assert.Equal(state.Log.Count, back.Log.Count);
        for (int i = 0; i < back.Log.Count; i++)
        {
            Assert.Equal(state.Log.Entries[i].Step, back.Log.Entries[i].Step);
            Assert.Equal(state.Log.Entries[i].Command, back.Log.Entries[i].Command);
        }
    }

    [Fact]
    public void TEC_008_TEC_012_random_values_come_from_one_source_held_in_the_state()
    {
        var state = new GameState(77);
        var reference = new SeededRandom(77);
        Assert.Equal(77UL, state.Random.Position);

        state.ApplyCommand(new RollIntoTallyCommand(6));
        state.ApplyCommand(new RollIntoTallyCommand(6));

        var expected = (reference.NextInt(6) + 1) + (reference.NextInt(6) + 1);
        Assert.Equal(expected, state.Tally);
        Assert.Equal(reference.Position, state.Random.Position);
    }

    [Fact]
    public void TEC_008_random_position_is_part_of_the_state_content_hashed()
    {
        var state = new GameState(5);
        Assert.Contains("random=5\n", state.CanonicalText());

        state.ApplyCommand(new RollIntoTallyCommand(6));

        Assert.NotEqual(5UL, state.Random.Position);
        Assert.Contains("random=" + state.Random.Position + "\n", state.CanonicalText());
    }

    [Fact]
    public void TEC_008_log_is_hashed_as_a_chained_digest_not_the_whole_log()
    {
        var a = Run(1, new ICommand[] { new AddToTallyCommand(1) });
        var b = Run(1, Enumerable.Repeat<ICommand>(new AddToTallyCommand(1), 500));

        Assert.True(b.CanonicalText().Length < a.CanonicalText().Length + 20);
        Assert.NotEqual(a.CanonicalText().Split('\n').Last(x => x.Length > 0), b.CanonicalText().Split('\n').Last(x => x.Length > 0));
    }

    [Fact]
    public void TEC_008_the_log_digest_depends_on_command_order()
    {
        var x = new AddToTallyCommand(1);
        var y = new RollIntoTallyCommand(2);
        var ab = Run(1, new ICommand[] { x, y });
        var ba = Run(1, new ICommand[] { y, x });

        Assert.NotEqual(ab.CanonicalText(), ba.CanonicalText());
    }

    [Fact]
    public void TEC_008_every_command_type_in_the_core_is_registered_and_round_trips()
    {
        var registry = CommandRegistry.CreateDefault();
        var types = typeof(ICommand).Assembly.GetTypes()
            .Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .ToList();
        Assert.NotEmpty(types);

        foreach (var t in types)
        {
            var ctor = Assert.Single(t.GetConstructors());
            var args = ctor.GetParameters()
                .Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)
                .ToArray();
            var command = (ICommand)ctor.Invoke(args);

            Assert.True(registry.IsRegistered(command.Type), t.Name + " is not registered in CreateDefault");
            var back = registry.ReadCommand(registry.WriteCommand(command));
            Assert.Equal(t, back.GetType());
            Assert.Equal(command, back);
        }
    }

    [Fact]
    public void TEC_008_a_command_using_the_reserved_type_key_is_refused_by_log_and_hash_alike()
    {
        var state = new GameState(1);

        Assert.Throws<InvalidOperationException>(() => state.ApplyCommand(new BadCommand()));
        Assert.Equal(0, state.Log.Count);
        Assert.Throws<InvalidOperationException>(() => CommandRegistry.CreateDefault().WriteCommand(new BadCommand()));
    }

    private sealed class BadCommand : ICommand
    {
        public string Type => "bad";
        public string? Validate(GameState state) => null;
        public void Apply(GameState state) { }
        public Dictionary<string, object?> ToJson() => new Dictionary<string, object?> { ["type"] = "x" };
    }

    [Fact]
    public void TEC_008_log_file_carries_format_and_seed_as_decimal_string()
    {
        var registry = CommandRegistry.CreateDefault();
        var log = new GameState(1).Log;

        var text = registry.WriteLog(ulong.MaxValue, log);

        Assert.Equal("{\"entries\":[],\"format\":\"weirdwar.log.v1\",\"seed\":\"18446744073709551615\"}", text);
        Assert.Equal(ulong.MaxValue, registry.ReadLog(text).Seed);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"entries\":[],\"format\":\"weirdwar.log.v2\",\"seed\":\"1\"}")]
    [InlineData("{\"entries\":[],\"format\":\"weirdwar.log.v1\"}")]
    [InlineData("{\"entries\":[],\"format\":\"weirdwar.log.v1\",\"seed\":1}")]
    [InlineData("{\"entries\":[],\"format\":\"weirdwar.log.v1\",\"seed\":\"-1\"}")]
    [InlineData("{\"entries\":[],\"format\":\"weirdwar.log.v1\",\"seed\":\"18446744073709551616\"}")]
    public void TEC_008_reading_a_bad_log_file_throws(string json)
    {
        Assert.Throws<JsonException>(() => CommandRegistry.CreateDefault().ReadLog(json));
    }

    [Fact]
    public void TEC_008_splitmix64_matches_pinned_reference_values()
    {
        var r = new SeededRandom(0);

        Assert.Equal(0xE220A8397B1DCDAFUL, r.NextUInt64());
        Assert.Equal(0x6E789E6AA1B965F4UL, r.NextUInt64());
        Assert.Equal(0x06C45D188009454FUL, r.NextUInt64());
        Assert.Equal(unchecked(3 * 0x9E3779B97F4A7C15UL), r.Position);
    }

    [Fact]
    public void TEC_008_next_int_stays_in_range_and_rejects_bad_bounds()
    {
        var r = new SeededRandom(123);
        for (int i = 0; i < 1000; i++)
        {
            var v = r.NextInt(7);
            Assert.InRange(v, 0, 6);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(0));
    }

    [Fact]
    public void TEC_008_DLV_012_same_seed_and_commands_give_identical_hash()
    {
        Assert.Equal(Run(42, Sequence).ComputeHash(), Run(42, Sequence).ComputeHash());
    }

    [Fact]
    public void TEC_008_different_commands_or_seed_give_different_hash()
    {
        var baseline = Run(42, Sequence).ComputeHash();

        Assert.NotEqual(baseline, Run(42, Sequence.Reverse()).ComputeHash());
        Assert.NotEqual(baseline, Run(42, Sequence.Take(3)).ComputeHash());
        Assert.NotEqual(baseline, Run(43, Sequence).ComputeHash());
    }

    [Fact]
    public void TEC_008_commands_with_the_same_tally_but_different_log_hash_differently()
    {
        var a = Run(1, new ICommand[] { new AddToTallyCommand(2), new AddToTallyCommand(3) });
        var b = Run(1, new ICommand[] { new AddToTallyCommand(5) });

        Assert.Equal(a.Tally, b.Tally);
        Assert.NotEqual(a.ComputeHash(), b.ComputeHash());
    }

    [Fact]
    public void DLV_012_hash_depends_only_on_content_pinned_value()
    {
        // Pinned: if this changes, the canonical serialisation changed (and so did every saved hash).
        var state = Run(42, Sequence);

        Assert.Equal(PinnedCanonicalText, state.CanonicalText());
        Assert.Equal(PinnedHash, state.ComputeHash());
    }

    [Fact]
    public void DLV_012_hash_does_not_depend_on_culture()
    {
        // A culture with a non-ASCII minus, a comma decimal and grouping, to catch any missing InvariantCulture.
        var odd = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
        odd.NumberFormat.NegativeSign = "\u2212";
        odd.NumberFormat.NumberDecimalSeparator = ",";
        odd.NumberFormat.NumberGroupSeparator = " ";
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            var expected = Run(42, Sequence).ComputeHash();
            var expectedText = Run(42, Sequence).CanonicalText();
            System.Globalization.CultureInfo.CurrentCulture = odd;
            Assert.Equal(expected, Run(42, Sequence).ComputeHash());
            Assert.Equal(PinnedCanonicalText, Run(42, Sequence).CanonicalText());
            Assert.Equal(expectedText, PinnedCanonicalText);
            Assert.Equal("{\"amount\":-1234567,\"type\":\"addToTally\"}", CommandRegistry.CreateDefault().WriteCommand(new AddToTallyCommand(-1234567)));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public void TEC_008_replaying_a_log_on_a_fresh_state_gives_the_same_hash()
    {
        var finished = new GameState(42);
        foreach (var c in Sequence) finished.ApplyCommand(c);
        finished.ApplyCommand(new AddToTallyCommand(-1000)); // rejected, not in log

        var replayed = GameState.Replay(42, finished.Log);

        Assert.Equal(finished.ComputeHash(), replayed.ComputeHash());
    }

    [Fact]
    public void TEC_008_replaying_a_log_read_from_json_gives_the_same_hash()
    {
        var registry = CommandRegistry.CreateDefault();
        var finished = Run(42, Sequence);

        var run = registry.ReadLog(registry.WriteLog(42, finished.Log));
        var replayed = GameState.Replay(run.Seed, run.Log);

        Assert.Equal(finished.ComputeHash(), replayed.ComputeHash());
    }

    [Fact]
    public void TEC_008_replay_throws_when_a_command_is_rejected()
    {
        var log = CommandLog.FromEntries(new[] { new LogEntry(0, new AddToTallyCommand(-1)) });

        Assert.Throws<ReplayDivergenceException>(() => GameState.Replay(1, log));
    }

    [Fact]
    public void TEC_008_replay_throws_when_the_log_is_out_of_order()
    {
        var log = CommandLog.FromEntries(new[]
        {
            new LogEntry(1, new AddToTallyCommand(1)), new LogEntry(0, new AddToTallyCommand(1)),
        });

        Assert.Throws<ReplayDivergenceException>(() => GameState.Replay(1, log));
    }

    private const string PinnedCanonicalText =
        "weirdwar.state.v1\nseed=42\nrandom=4354685564936845396\ntally=17\nmap=none\nsectiontypes=none\nlog=4\n" +
        "digest=1399ad3f82b6ed66680bbd32b287fefe2617d811358121eaa4c9028c6a704336\n";
    private const string PinnedHash = "40788c2ebaf517b15d949265f0856c3e8e8b192779b93b4ceeeadd794969de9d";
}
