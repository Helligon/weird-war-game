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

        var back = registry.ReadLog(registry.WriteLog(state.Log));

        Assert.Equal(state.Log.Count, back.Count);
        for (int i = 0; i < back.Count; i++)
        {
            Assert.Equal(state.Log.Entries[i].Step, back.Entries[i].Step);
            Assert.Equal(state.Log.Entries[i].Command, back.Entries[i].Command);
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
    public void TEC_008_random_position_is_part_of_the_state_hash()
    {
        var a = new GameState(5);
        var b = new GameState(5);
        Assert.Equal(a.ComputeHash(), b.ComputeHash());

        b.Random.NextUInt64();

        Assert.NotEqual(a.ComputeHash(), b.ComputeHash());
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
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            var expected = Run(42, Sequence).ComputeHash();
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.Equal(expected, Run(42, Sequence).ComputeHash());
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

        var replayed = GameState.Replay(42, registry.ReadLog(registry.WriteLog(finished.Log)));

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
        "weirdwar.state.v1\nseed=42\nrandom=4354685564936845396\ntally=17\nlog=4\n" +
        "0:{\"amount\":5,\"type\":\"addToTally\"}\n1:{\"sides\":6,\"type\":\"rollIntoTally\"}\n" +
        "2:{\"amount\":-2,\"type\":\"addToTally\"}\n3:{\"sides\":20,\"type\":\"rollIntoTally\"}\n";
    private const string PinnedHash = "89f5656eac201377f5cd2f2ffade25da08c1e7e1dd6a37eb7afa5fd11ef2c4e5";
}
