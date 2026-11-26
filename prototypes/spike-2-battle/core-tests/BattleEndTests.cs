using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

public class BattleEndTests
{
    // TEC-008: fixed stats, parameters and map written here, so tuning the files in data/ cannot break these tests.
    private const string StatsJson = "{ \"riflemen\": { \"speed\": 1.5, \"P-HIE-01\": 8, \"P-HIE-02\": 3, \"range\": 30, \"damage\": 1, \"accuracy\": 0.6, \"volleyInterval\": 2, \"coverAdjacencyDistance\": 3 },"
        + " \"officer\": { \"health\": 10, \"speed\": 4, \"rifleRange\": 80, \"rifleDamage\": 3, \"rifleHitRadius\": 3 } }";
    private const string MapJson = "{ \"width\": 100, \"height\": 60, \"cover\": ["
        + "{ \"id\": \"ruin-west\", \"x\": 30, \"y\": 15, \"radius\": 4 }, { \"id\": \"wall-centre\", \"x\": 50, \"y\": 32, \"radius\": 5 },"
        + "{ \"id\": \"copse-north\", \"x\": 45, \"y\": 50, \"radius\": 4 }, { \"id\": \"crater-east\", \"x\": 72, \"y\": 20, \"radius\": 4 },"
        + "{ \"id\": \"ruin-east\", \"x\": 68, \"y\": 45, \"radius\": 4 } ] }";
    private const string ParametersJson = "{\"P-CMD-01\": 2, \"P-CMD-03\": 0.5, \"P-DAI-01\": 25}";

    private static BattleState NewBattle(ulong seed = 1) =>
        BattleState.SetUp(BattleMapLoader.Load(MapJson), UnitStats.Load(StatsJson), BattleParameters.Load(ParametersJson), seed);

    private static void Kill(Section s) { s.TakeDamage(s.Health); }

    private static void Steps(BattleState b, int n) { for (var i = 0; i < n; i++) b.Step(); }

    [Fact]
    public void VIC_001_the_player_has_won_when_the_enemy_section_is_destroyed()
    {
        var b = NewBattle();
        Kill(b.Find(BattleState.EnemySectionId)!);
        Assert.Equal(BattleOutcome.PlayerWon, b.Outcome);
    }

    [Fact]
    public void VIC_001_the_enemy_has_won_when_every_friendly_unit_officer_and_section_is_destroyed()
    {
        var b = NewBattle();
        Kill(b.Find(BattleState.PlayerSectionId)!);
        Assert.Equal(BattleOutcome.InProgress, b.Outcome); // the Officer still lives
        b.Officers[0].TakeDamage(b.Officers[0].Health);
        Assert.Equal(BattleOutcome.EnemyWon, b.Outcome);
    }

    [Fact]
    public void VIC_001_a_destroyed_officer_alone_does_not_lose_the_battle()
    {
        var b = NewBattle();
        b.Officers[0].TakeDamage(b.Officers[0].Health);
        Assert.Equal(BattleOutcome.InProgress, b.Outcome);
    }

    [Fact]
    public void VIC_001_no_side_has_won_while_both_sides_have_a_living_unit()
    {
        Assert.Equal(BattleOutcome.InProgress, NewBattle().Outcome);
    }

    [Fact]
    public void VIC_001_both_sides_destroyed_at_once_is_a_draw_stand_in()
    {
        var b = NewBattle();
        Kill(b.Find(BattleState.PlayerSectionId)!);
        Kill(b.Find(BattleState.EnemySectionId)!);
        b.Officers[0].TakeDamage(b.Officers[0].Health);
        Assert.Equal(BattleOutcome.Draw, b.Outcome);
    }

    [Fact]
    public void VIC_001_a_finished_battle_no_longer_steps_or_takes_commands()
    {
        var b = NewBattle();
        Kill(b.Find(BattleState.EnemySectionId)!);
        var steps = b.StepCount;
        b.Step();
        Assert.Equal(steps, b.StepCount);
        var result = b.ApplyCommand(new OfficerMoveCommand(0, 1, 0));
        Assert.False(result.IsAccepted);
        Assert.Empty(b.CommandLog);
    }

    [Fact]
    public void VIC_004_a_long_battle_with_both_sides_alive_does_not_end_because_of_time()
    {
        // Both Sections hold still, out of range of each other; an hour of battle time passes.
        var b = NewBattle();
        Assert.True(b.ApplyCommand(new StopCommand(BattleState.PlayerSectionId)).IsAccepted);
        Assert.True(b.ApplyCommand(new StopCommand(BattleState.EnemySectionId)).IsAccepted);
        Steps(b, 36000);
        Assert.Equal(BattleOutcome.InProgress, b.Outcome);
        Assert.Equal(36000, b.StepCount);
    }

    [Fact]
    public void VIC_004_CMD_002_a_paused_battle_that_is_not_stepped_does_not_change()
    {
        // The tactical view pauses by not calling Step(). Nothing else moves the battle on, and reading it changes nothing.
        var b = NewBattle();
        Steps(b, 50);
        var before = BattleDigest(b);
        _ = b.Outcome;
        _ = CommandLogFile.Write(b);
        Assert.Equal(before, BattleDigest(b));
    }

    private static string BattleDigest(BattleState b)
    {
        var parts = new List<string>();
        foreach (var s in b.Sections) parts.Add(s.Id + ":" + s.Health + "@" + s.Position.X + "," + s.Position.Y);
        foreach (var o in b.Officers) parts.Add("O" + o.Health + "@" + o.Position.X + "," + o.Position.Y);
        return string.Join("|", parts) + "#" + b.StepCount + "#" + b.CommandLog.Count;
    }

    // Plays a battle with every kind of command, until it ends or runs out of steps.
    private static BattleState PlayToEnd(ulong seed)
    {
        var b = NewBattle(seed);
        b.ApplyCommand(new MoveCommand(BattleState.PlayerSectionId, new[] { new Waypoint(40, 30) }));
        b.ApplyCommand(new OfficerMoveCommand(0, 1, 0));
        Steps(b, 30);
        b.ApplyCommand(new OfficerMoveCommand(0, 0, 0));
        b.ApplyCommand(new OfficerAimCommand(0, 0.0));
        b.ApplyCommand(new OfficerFireCommand(0));
        b.ApplyCommand(new TargetCommand(BattleState.PlayerSectionId, BattleState.EnemySectionId));
        for (var i = 0; i < 20000 && b.Outcome == BattleOutcome.InProgress; i++)
        {
            if (i % 25 == 0) b.ApplyCommand(new OfficerFireCommand(0));
            b.Step();
        }
        return b;
    }

    [Fact]
    public void TEC_005_PCH_009_the_written_log_has_every_command_in_order_with_step_and_time()
    {
        var b = PlayToEnd(1);
        Assert.NotEqual(BattleOutcome.InProgress, b.Outcome);
        var text = CommandLogFile.Write(b);
        var read = CommandLogFile.Read(text);
        Assert.Equal(b.CommandLog.Count, read.Commands.Count);
        Assert.True(read.Commands.Count > 5);
        Assert.Contains(read.Commands, c => c.Command is OfficerMoveCommand);
        Assert.Contains(read.Commands, c => c.Command is OfficerAimCommand);
        Assert.Contains(read.Commands, c => c.Command is OfficerFireCommand);
        Assert.Contains(read.Commands, c => c.Command is MoveCommand);
        Assert.Contains(read.Commands, c => c.Command is TargetCommand);
        Assert.Contains("\"step\":30,\"time\":3,", text); // step 30 is applied at 3 s of battle time
        Assert.Equal(b.StepCount, read.Steps);
        Assert.Equal(BattleState.StepSeconds, read.StepSeconds);
        Assert.Equal(b.Outcome, read.Outcome);
    }

    [Fact]
    public void TEC_008_every_command_read_back_equals_the_one_issued()
    {
        var b = PlayToEnd(1);
        var read = CommandLogFile.Read(CommandLogFile.Write(b));
        for (var i = 0; i < b.CommandLog.Count; i++)
        {
            Assert.Equal(b.CommandLog[i].Step, read.Commands[i].Step);
            Assert.Equal(b.CommandLog[i].Command, read.Commands[i].Command);
        }
    }

    [Fact]
    public void TEC_008_the_log_text_is_the_same_every_time_for_the_same_battle()
    {
        Assert.Equal(CommandLogFile.Write(PlayToEnd(1)), CommandLogFile.Write(PlayToEnd(1)));
    }

    [Fact]
    public void TEC_005_a_replay_of_the_written_log_reaches_the_same_end_state()
    {
        var original = PlayToEnd(1);
        var read = CommandLogFile.Read(CommandLogFile.Write(original));
        var replay = NewBattle(1);
        replay.Replay(read.Commands, read.Steps);
        Assert.Equal(BattleDigest(original), BattleDigest(replay));
        Assert.Equal(original.Outcome, replay.Outcome);
    }

    [Fact]
    public void TEC_008_a_log_that_is_not_a_log_is_refused()
    {
        Assert.Throws<CommandFormatException>(() => CommandLogFile.Read("[]"));
        Assert.Throws<CommandFormatException>(() => CommandLogFile.Read("{\"stepSeconds\":0.1,\"steps\":1,\"outcome\":\"Nope\",\"commands\":[]}"));
        Assert.Throws<CommandFormatException>(() => CommandLogFile.Read("{\"stepSeconds\":0.1,\"steps\":1,\"outcome\":\"Draw\"}"));
    }
}
