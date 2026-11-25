using System.Reflection;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

public class OfficerTests
{
    // TEC-008: fixed stats, parameters and map written here, so tuning the files in data/ cannot break these tests.
    private const string StatsJson = "{ \"riflemen\": { \"speed\": 1.5, \"P-HIE-01\": 8, \"P-HIE-02\": 3, \"range\": 30, \"damage\": 1, \"accuracy\": 0.6, \"volleyInterval\": 2, \"P-DAI-01\": 25, \"coverAdjacencyDistance\": 3 },"
        + " \"officer\": { \"health\": 10, \"speed\": 4, \"rifleRange\": 80, \"rifleDamage\": 3, \"rifleHitRadius\": 3 } }";
    private const string MapJson = "{ \"width\": 100, \"height\": 60, \"cover\": ["
        + "{ \"id\": \"ruin-west\", \"x\": 30, \"y\": 15, \"radius\": 4 }, { \"id\": \"wall-centre\", \"x\": 50, \"y\": 32, \"radius\": 5 },"
        + "{ \"id\": \"copse-north\", \"x\": 45, \"y\": 50, \"radius\": 4 }, { \"id\": \"crater-east\", \"x\": 72, \"y\": 20, \"radius\": 4 },"
        + "{ \"id\": \"ruin-east\", \"x\": 68, \"y\": 45, \"radius\": 4 } ] }";
    private const string ParametersJson = "{\"P-CMD-01\": 2, \"P-CMD-03\": 0.5}";

    private static string Text(string file) => file == "stats.json" ? StatsJson : file == "map.json" ? MapJson : ParametersJson;
    private static UnitStats Stats() => UnitStats.Load(StatsJson);

    private static BattleState NewBattle(ulong seed = 1) =>
        BattleState.SetUp(BattleMapLoader.Load(MapJson), Stats(), BattleParameters.Load(ParametersJson), seed);

    private static void Steps(BattleState b, int n) { for (var i = 0; i < n; i++) b.Step(); }

    // Walks the Officer east for 3 s of battle time (about 12 m), then stops. The enemy is then inside rifle range and out of its own.
    private static void CloseIn(BattleState b)
    {
        Assert.True(b.ApplyCommand(new OfficerMoveCommand(0, 1, 0)).IsAccepted);
        Steps(b, 30);
        Assert.True(b.ApplyCommand(new OfficerMoveCommand(0, 0, 0)).IsAccepted);
    }

    // Aims the Officer at the enemy Section.
    private static void FaceEnemy(BattleState b)
    {
        var enemy = b.Find(BattleState.EnemySectionId)!;
        var o = b.Officers[0];
        var dx = enemy.Position.X - o.Position.X;
        var dy = enemy.Position.Y - o.Position.Y;
        Assert.True(b.ApplyCommand(new OfficerAimCommand(0, Math.Atan2(dy, dx))).IsAccepted);
    }

    [Fact]
    public void PCH_001_move_command_walks_the_officer_at_stats_speed_one_step_at_a_time()
    {
        var b = NewBattle();
        var x0 = b.Officers[0].Position.X;
        Assert.True(b.ApplyCommand(new OfficerMoveCommand(0, 1, 0)).IsAccepted);
        Steps(b, 20); // 2 s
        Assert.Equal(x0 + Stats().OfficerSpeed * 2, b.Officers[0].Position.X, 6);
    }

    [Fact]
    public void TEC_008_a_move_command_does_not_move_the_officer_until_the_battle_steps()
    {
        var b = NewBattle();
        var p = b.Officers[0].Position;
        b.ApplyCommand(new OfficerMoveCommand(0, 1, 0));
        Assert.Equal(p.X, b.Officers[0].Position.X);
        b.Step();
        Assert.True(b.Officers[0].Position.X > p.X);
    }

    [Fact]
    public void PCH_001_the_officer_keeps_walking_until_a_new_move_command_changes_the_direction()
    {
        var b = NewBattle();
        b.ApplyCommand(new OfficerMoveCommand(0, 1, 0));
        Steps(b, 10);
        var x1 = b.Officers[0].Position.X;
        Steps(b, 10);
        Assert.True(b.Officers[0].Position.X > x1);
        b.ApplyCommand(new OfficerMoveCommand(0, 0, 0));
        var x2 = b.Officers[0].Position.X;
        Steps(b, 10);
        Assert.Equal(x2, b.Officers[0].Position.X);
    }

    [Fact]
    public void TEC_008_the_speed_cap_holds_whatever_direction_the_client_sends()
    {
        var b = NewBattle();
        var p0 = b.Officers[0].Position;
        b.ApplyCommand(new OfficerMoveCommand(0, 500, 500));
        Steps(b, 10); // 1 s
        var p1 = b.Officers[0].Position;
        var dist = Math.Sqrt(Math.Pow(p1.X - p0.X, 2) + Math.Pow(p1.Y - p0.Y, 2));
        Assert.Equal(Stats().OfficerSpeed, dist, 6);
    }

    [Fact]
    public void TEC_008_only_step_advances_time_because_tick_is_not_public()
    {
        Assert.Null(typeof(BattleState).GetMethod("Tick", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(BattleState).GetMethod("Step", BindingFlags.Instance | BindingFlags.Public));
        Assert.Null(typeof(OfficerMoveCommand).GetProperty("Seconds"));
    }

    [Fact]
    public void PCH_001_officer_cannot_leave_the_map()
    {
        var b = NewBattle();
        b.ApplyCommand(new OfficerMoveCommand(0, -1, 0));
        Steps(b, 200); // 20 s at 4 m/s is more than the 5 m to the west edge
        Assert.Equal(0, b.Officers[0].Position.X);
    }

    [Fact]
    public void PCH_001_aim_command_turns_the_officer()
    {
        var b = NewBattle();
        Assert.True(b.ApplyCommand(new OfficerAimCommand(0, 1.25)).IsAccepted);
        Assert.Equal(1.25, b.Officers[0].AimRadians);
    }

    [Fact]
    public void PCH_001_rifle_hits_and_damages_an_enemy_unit()
    {
        var b = NewBattle();
        CloseIn(b);
        FaceEnemy(b);
        var enemy = b.Find(BattleState.EnemySectionId)!;
        var unitsBefore = enemy.UnitCount;
        var r = b.ApplyCommand(new OfficerFireCommand(0));
        Assert.True(r.IsAccepted);
        Assert.True(r.Shot!.Value.Hit);
        Assert.Equal(BattleState.EnemySectionId, r.Shot.Value.Target);
        Assert.Equal(Stats().OfficerRifleDamage, r.Shot.Value.Damage);
        Assert.True(enemy.UnitCount < unitsBefore);
    }

    [Fact]
    public void PCH_001_rifle_passes_through_a_friendly_section_in_the_line_of_fire()
    {
        var b = NewBattle();
        CloseIn(b);
        FaceEnemy(b);
        var o = b.Officers[0].Position;
        var friend = new Section(new SectionId(3), Side.Player, UnitType.Riflemen, 8, 3, new MapPoint(o.X + 5, o.Y));
        b.Add(friend);
        var enemy = b.Find(BattleState.EnemySectionId)!;
        var friendHealth = friend.Health;
        var enemyHealth = enemy.Health;

        var shot = b.ApplyCommand(new OfficerFireCommand(0)).Shot!.Value;

        Assert.Equal(friendHealth, friend.Health);
        Assert.Equal(BattleState.EnemySectionId, shot.Target);
        Assert.True(enemy.Health < enemyHealth);
    }

    [Fact]
    public void PCH_001_rifle_misses_when_aimed_away_and_damages_nothing()
    {
        var b = NewBattle();
        CloseIn(b);
        b.ApplyCommand(new OfficerAimCommand(0, Math.PI));
        var enemy = b.Find(BattleState.EnemySectionId)!;
        var health = enemy.Health;
        var r = b.ApplyCommand(new OfficerFireCommand(0));
        Assert.False(r.Shot!.Value.Hit);
        Assert.Equal(health, enemy.Health);
    }

    [Fact]
    public void PCH_001_rifle_misses_beyond_range()
    {
        var b = NewBattle();
        FaceEnemy(b);
        var enemy = b.Find(BattleState.EnemySectionId)!;
        // At the spawn point the enemy is further away than the rifle range.
        var distance = enemy.Position.X - b.Officers[0].Position.X;
        Assert.True(distance > Stats().OfficerRifleRange);
        Assert.False(b.ApplyCommand(new OfficerFireCommand(0)).Shot!.Value.Hit);
    }

    [Fact]
    public void DAI_006_an_officer_shot_makes_the_section_prefer_the_officer_as_its_target()
    {
        var b = NewBattle();
        // An enemy Section with a nearer player Section (10 m) and a farther player Officer (25 m), both inside its range.
        var near = new Section(new SectionId(3), Side.Player, UnitType.Riflemen, 8, 3, new MapPoint(40, 30));
        var enemy = new Section(new SectionId(4), Side.Enemy, UnitType.Riflemen, 8, 3, new MapPoint(50, 30));
        b.Add(near);
        b.Add(enemy);
        b.AddOfficer(new Officer(Side.Player, 10, new MapPoint(25, 30)));
        var officer = b.Officers[1];

        var before = DefaultBehaviour.ChooseTarget(b, enemy)!.Value;
        Assert.Same(near, before.Section);

        b.ApplyCommand(new OfficerAimCommand(1, 0));
        var shot = b.ApplyCommand(new OfficerFireCommand(1)).Shot!.Value;
        Assert.Equal(enemy.Id, shot.Target);

        Assert.Same(officer, enemy.LastAttacker!.Value.Officer);
        var after = DefaultBehaviour.ChooseTarget(b, enemy)!.Value;
        Assert.Same(officer, after.Officer);
    }

    [Fact]
    public void DAI_006_a_section_hit_by_an_officer_shoots_back_at_it_when_the_battle_runs()
    {
        var b = NewBattle();
        var enemy = new Section(new SectionId(4), Side.Enemy, UnitType.Riflemen, 8, 3, new MapPoint(40, 30));
        b.Add(enemy);
        b.AddOfficer(new Officer(Side.Player, 1000, new MapPoint(25, 30)));
        var officer = b.Officers[1];
        b.ApplyCommand(new OfficerAimCommand(1, 0));
        b.ApplyCommand(new OfficerFireCommand(1));

        Steps(b, 30);

        Assert.True(officer.Health < 1000);
    }

    [Fact]
    public void PCH_001_rifle_is_deterministic_for_the_same_commands()
    {
        int Run()
        {
            var b = NewBattle(7);
            CloseIn(b);
            FaceEnemy(b);
            b.ApplyCommand(new OfficerFireCommand(0));
            b.ApplyCommand(new OfficerFireCommand(0));
            return b.Find(BattleState.EnemySectionId)!.Health;
        }
        Assert.Equal(Run(), Run());
    }

    private static readonly MapPoint Still = new MapPoint(0, 0);

    [Fact]
    public void PCH_009_input_for_move_aim_and_fire_becomes_one_command_each()
    {
        var commands = OfficerInputMapper.Map(0, 0, Still, forward: 1, strafe: 0, lookDelta: 0.1, fire: true);
        Assert.Collection(commands,
            c => Assert.IsType<OfficerAimCommand>(c),
            c => Assert.IsType<OfficerMoveCommand>(c),
            c => Assert.IsType<OfficerFireCommand>(c));
    }

    [Fact]
    public void PCH_009_no_input_makes_no_commands()
    {
        Assert.Empty(OfficerInputMapper.Map(0, 0, Still, 0, 0, 0, false));
    }

    [Fact]
    public void PCH_009_a_held_key_sends_one_move_command_not_one_per_frame()
    {
        var b = NewBattle();
        var first = OfficerInputMapper.Map(0, 0, b.Officers[0].MoveDirection, 1, 0, 0, false);
        Assert.Single(first);
        foreach (var c in first) b.ApplyCommand(c);
        // Same keys, next frames: nothing to send.
        Assert.Empty(OfficerInputMapper.Map(0, 0, b.Officers[0].MoveDirection, 1, 0, 0, false));
        Steps(b, 3);
        Assert.Empty(OfficerInputMapper.Map(0, 0, b.Officers[0].MoveDirection, 1, 0, 0, false));
        // Releasing the key sends one stop.
        var stop = Assert.IsType<OfficerMoveCommand>(Assert.Single(OfficerInputMapper.Map(0, 0, b.Officers[0].MoveDirection, 0, 0, 0, false)));
        Assert.Equal((0.0, 0.0), (stop.DirectionX, stop.DirectionY));
    }

    [Fact]
    public void PCH_009_a_held_diagonal_is_not_resent_each_frame()
    {
        var b = NewBattle();
        foreach (var c in OfficerInputMapper.Map(0, 0, Still, 1, 1, 0, false)) b.ApplyCommand(c);
        Assert.Empty(OfficerInputMapper.Map(0, 0, b.Officers[0].MoveDirection, 1, 1, 0, false));
    }

    [Fact]
    public void PCH_009_forward_moves_along_the_aim_and_strafe_moves_to_its_right()
    {
        var north = Math.PI / 2;
        var fwd = (OfficerMoveCommand)OfficerInputMapper.Map(0, north, Still, 1, 0, 0, false).Single();
        Assert.Equal(0, fwd.DirectionX, 9);
        Assert.Equal(1, fwd.DirectionY, 9);
        var right = (OfficerMoveCommand)OfficerInputMapper.Map(0, north, Still, 0, 1, 0, false).Single();
        Assert.Equal(1, right.DirectionX, 9);
        Assert.Equal(0, right.DirectionY, 9);
    }

    [Fact]
    public void PCH_009_move_aim_and_fire_round_trip_through_json()
    {
        var commands = new IBattleCommand[]
        {
            new OfficerMoveCommand(0, 0.70710678118654757, -0.1),
            new OfficerAimCommand(0, -2.5),
            new OfficerFireCommand(0),
        };
        foreach (var c in commands)
        {
            var json = CommandSerialiser.Serialise(c);
            var back = CommandSerialiser.Deserialise(json);
            Assert.Equal(c, back);
            Assert.Equal(json, CommandSerialiser.Serialise(back));
        }
    }

    [Fact]
    public void PCH_009_whole_number_values_round_trip_through_json()
    {
        var c = new OfficerMoveCommand(0, 1, 0);
        Assert.Equal(c, CommandSerialiser.Deserialise(CommandSerialiser.Serialise(c)));
    }

    [Fact]
    public void PCH_009_accepted_commands_are_recorded_with_their_step_number_in_order()
    {
        var b = NewBattle();
        var aim = new OfficerAimCommand(0, 0.5);
        var move = new OfficerMoveCommand(0, 1, 0);
        var fire = new OfficerFireCommand(0);
        b.ApplyCommand(aim);
        Steps(b, 3);
        b.ApplyCommand(move);
        Steps(b, 2);
        b.ApplyCommand(fire);
        Assert.Equal(new[] { (0, (IBattleCommand)aim), (3, move), (5, fire) }, b.CommandLog.Select(l => (l.Step, l.Command)).ToArray());
        Assert.Equal(5, b.StepCount);
    }

    [Fact]
    public void PCH_009_rejected_commands_change_nothing_and_are_not_logged()
    {
        var b = NewBattle();
        var p = b.Officers[0].Position;
        Assert.False(b.ApplyCommand(new OfficerMoveCommand(0, double.NaN, 0)).IsAccepted);
        Assert.False(b.ApplyCommand(new OfficerMoveCommand(0, double.PositiveInfinity, 0)).IsAccepted);
        Assert.False(b.ApplyCommand(new OfficerAimCommand(0, double.PositiveInfinity)).IsAccepted);
        Assert.False(b.ApplyCommand(new OfficerFireCommand(5)).IsAccepted);
        Steps(b, 5);
        Assert.Equal(p.X, b.Officers[0].Position.X);
        Assert.Empty(b.CommandLog);
    }

    [Fact]
    public void PCH_003_a_dead_officer_rejects_move_aim_and_fire()
    {
        var b = NewBattle();
        b.Officers[0].TakeDamage(1000);
        Assert.True(b.Officers[0].IsDestroyed);
        var aimBefore = b.Officers[0].AimRadians;
        foreach (var c in new IBattleCommand[] { new OfficerMoveCommand(0, 1, 0), new OfficerAimCommand(0, 1), new OfficerFireCommand(0) })
        {
            var r = b.ApplyCommand(c);
            Assert.False(r.IsAccepted);
            Assert.Contains("destroyed", r.Reason);
        }
        Assert.Empty(b.CommandLog);
        Assert.Equal(aimBefore, b.Officers[0].AimRadians);
        Assert.Equal(0, b.Officers[0].MoveDirection.X);
    }

    [Fact]
    public void PCH_003_an_officer_that_dies_while_walking_stops()
    {
        var b = NewBattle();
        b.ApplyCommand(new OfficerMoveCommand(0, 1, 0));
        Steps(b, 5);
        b.Officers[0].TakeDamage(1000);
        var x = b.Officers[0].Position.X;
        Steps(b, 5);
        Assert.Equal(x, b.Officers[0].Position.X);
    }

    [Fact]
    public void DLV_013_officer_state_has_no_public_setters()
    {
        foreach (var name in new[] { nameof(Officer.Position), nameof(Officer.AimRadians), nameof(Officer.MoveDirection) })
        {
            var setter = typeof(Officer).GetProperty(name)!.SetMethod;
            Assert.True(setter == null || !setter.IsPublic, name + " must not be settable from outside the core.");
        }
    }

    private sealed class ScriptResult
    {
        public int Scripted;
        public int Hits;
        public bool SectionNamedOfficerAsAttacker;
    }

    // Aims at the enemy Section from where the Officer stands now. The angle is logged, so replay does not recompute it.
    private static void AimAtEnemy(BattleState b, ScriptResult r)
    {
        var e = b.Find(BattleState.EnemySectionId)!.Position;
        var o = b.Officers[0].Position;
        Do(b, r, new OfficerAimCommand(0, Math.Atan2(e.Y - o.Y, e.X - o.X)));
    }

    private static void Do(BattleState b, ScriptResult r, IBattleCommand c)
    {
        r.Scripted++;
        var result = b.ApplyCommand(c);
        Assert.True(result.IsAccepted, "scripted command rejected at step " + b.StepCount + ": " + result.Reason);
        if (result.Shot is OfficerShot shot && shot.Hit)
        {
            r.Hits++;
            if (b.Find(BattleState.EnemySectionId)!.LastAttacker?.Officer == b.Officers[0]) r.SectionNamedOfficerAsAttacker = true;
        }
    }

    // A fixed script of Officer commands at fixed steps, run while both Sections follow their default behaviour (DAI-001..006).
    // The Officer walks east, stops about 55 m from the enemy (outside its 30 m range), shoots it three times, then backs away.
    private static ScriptResult RunScript(BattleState b, int totalSteps)
    {
        var r = new ScriptResult();
        for (var step = 0; step < totalSteps; step++)
        {
            if (step == 0) Do(b, r, new OfficerMoveCommand(0, 1, 0));
            if (step == 60) Do(b, r, new OfficerMoveCommand(0, 0, 0));
            if (step == 61 || step == 100 || step == 150) { AimAtEnemy(b, r); Do(b, r, new OfficerFireCommand(0)); }
            if (step == 200) Do(b, r, new OfficerMoveCommand(0, -1, 0));
            if (step == 230) Do(b, r, new OfficerMoveCommand(0, 0, 0));
            b.Step();
        }
        return r;
    }

    private static void AssertSameBattle(BattleState a, BattleState c)
    {
        foreach (var id in new[] { BattleState.PlayerSectionId, BattleState.EnemySectionId })
        {
            var x = a.Find(id)!; var y = c.Find(id)!;
            Assert.Equal(x.Position.X, y.Position.X);
            Assert.Equal(x.Position.Y, y.Position.Y);
            Assert.Equal(x.Health, y.Health);
        }
        Assert.Equal(a.Officers[0].Position.X, c.Officers[0].Position.X);
        Assert.Equal(a.Officers[0].Position.Y, c.Officers[0].Position.Y);
        Assert.Equal(a.Officers[0].AimRadians, c.Officers[0].AimRadians);
        Assert.Equal(a.Officers[0].Health, c.Officers[0].Health);
        Assert.Equal(a.StepCount, c.StepCount);
    }

    [Fact]
    public void TEC_008_replaying_the_step_numbered_log_reproduces_a_battle_with_default_behaviour_and_officer_commands()
    {
        const int steps = 600;
        var a = NewBattle(5);
        var spawn = a.Officers[0].Position;
        var script = RunScript(a, steps);

        // The script really exercised the risky path: the Officer hit the enemy, the enemy named the Officer as its attacker,
        // the Officer survived, and every scripted command was accepted and logged.
        Assert.True(script.Hits >= 1, "no Officer shot hit");
        Assert.True(script.SectionNamedOfficerAsAttacker, "the hit Section did not name the Officer as its attacker");
        Assert.True(a.Officers[0].Health > 0, "the Officer died, so later commands would be rejected");
        Assert.Equal(script.Scripted, a.CommandLog.Count);
        Assert.Equal(10, script.Scripted); // move, stop, 3 x (aim, fire), move, stop
        Assert.True(a.Find(BattleState.EnemySectionId)!.Health < a.Stats.RiflemenMaxUnits * a.Stats.RiflemenHealthPerUnit);
        // 6 s east, then 3 s back, both at the Officer's speed: it ends 3 s of walking east of where it spawned.
        Assert.Equal(spawn.X + Stats().OfficerSpeed * (6.0 - 3.0), a.Officers[0].Position.X, 6);

        // Round-trip every command through JSON, as a saved log would be.
        var saved = a.CommandLog.Select(l => new LoggedCommand(l.Step, CommandSerialiser.Deserialise(CommandSerialiser.Serialise(l.Command)))).ToList();
        var c = NewBattle(5);
        c.Replay(saved, steps);

        AssertSameBattle(a, c);
        Assert.Equal(a.CommandLog.Count, c.CommandLog.Count);
        Assert.Equal(a.CommandLog.Select(l => l.Step), c.CommandLog.Select(l => l.Step));
    }

    [Fact]
    public void TEC_008_a_different_seed_changes_the_replayed_battle_so_the_replay_test_has_teeth()
    {
        var a = NewBattle(5);
        RunScript(a, 600);
        var c = NewBattle(6);
        c.Replay(a.CommandLog, 600);
        var same = a.Find(BattleState.EnemySectionId)!.Health == c.Find(BattleState.EnemySectionId)!.Health
            && a.Find(BattleState.PlayerSectionId)!.Health == c.Find(BattleState.PlayerSectionId)!.Health
            && a.Officers[0].Health == c.Officers[0].Health;
        Assert.False(same);
    }

    [Fact]
    public void TEC_008_replay_throws_when_a_logged_command_is_rejected_instead_of_ignoring_it()
    {
        var b = NewBattle();
        var bad = new[] { new LoggedCommand(3, new OfficerFireCommand(5)) }; // there is no Officer 5
        var ex = Assert.Throws<InvalidOperationException>(() => b.Replay(bad, 10));
        Assert.Contains("step 3", ex.Message);

        // A command that was accepted when logged is a divergence if the Officer is dead on replay.
        var dead = NewBattle();
        dead.Officers[0].TakeDamage(1000);
        Assert.Throws<InvalidOperationException>(() => dead.Replay(new[] { new LoggedCommand(0, new OfficerAimCommand(0, 1)) }, 5));
    }

    [Fact]
    public void TEC_008_replay_rejects_a_command_logged_after_the_last_step()
    {
        var b = NewBattle();
        Assert.Throws<InvalidOperationException>(() => b.Replay(new[] { new LoggedCommand(11, new OfficerAimCommand(0, 1)) }, 10));
        // A command logged at the final step count is applied, without a further step.
        var c = NewBattle();
        c.Replay(new[] { new LoggedCommand(10, new OfficerAimCommand(0, 1)) }, 10);
        Assert.Equal(1, c.Officers[0].AimRadians);
        Assert.Equal(10, c.StepCount);
    }

    private static IEnumerable<Type> OfficerCommandTypes() =>
        typeof(IBattleCommand).Assembly.GetTypes().Where(t => typeof(OfficerCommand).IsAssignableFrom(t) && !t.IsAbstract);

    private static bool MentionsSectionOrUnit(Type t) =>
        t.GetProperties().Any(p => p.PropertyType == typeof(SectionId) || p.PropertyType == typeof(Section)
            || p.Name.Contains("Section") || p.Name.Contains("Unit"));

    [Fact]
    public void PCH_004_no_officer_command_names_a_section_or_a_unit()
    {
        foreach (var t in OfficerCommandTypes())
        {
            Assert.False(MentionsSectionOrUnit(t), t.Name);
            Assert.False(typeof(SectionCommand).IsAssignableFrom(t), t.Name);
        }
    }

    [Fact]
    public void PCH_004_officer_commands_leave_player_sections_untouched()
    {
        var b = NewBattle();
        var mine = b.Find(BattleState.PlayerSectionId)!;
        CloseIn(b);
        var (x, y, h) = (mine.Position.X, mine.Position.Y, mine.Health);
        FaceEnemy(b);
        b.ApplyCommand(new OfficerFireCommand(0));
        Assert.Equal((x, y, h), (mine.Position.X, mine.Position.Y, mine.Health));
    }

    [Fact]
    public void PCH_004_a_section_command_leaves_the_officer_untouched()
    {
        var b = NewBattle();
        var o = b.Officers[0];
        var (x, y, aim, hp) = (o.Position.X, o.Position.Y, o.AimRadians, o.Health);
        Assert.True(b.ApplyCommand(new StopCommand(BattleState.PlayerSectionId)).IsAccepted);
        Assert.Equal((x, y, aim, hp), (o.Position.X, o.Position.Y, o.AimRadians, o.Health));
    }

    [Fact]
    public void PCH_004_the_input_mapper_only_makes_officer_commands()
    {
        var all = OfficerInputMapper.Map(0, 0.3, Still, 1, 1, 0.2, true);
        Assert.All(all, c => Assert.IsAssignableFrom<OfficerCommand>(c));
    }

    [Fact]
    public void PCH_008_the_only_officer_commands_are_move_aim_and_fire()
    {
        var names = OfficerCommandTypes().Select(t => t.Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { nameof(OfficerAimCommand), nameof(OfficerFireCommand), nameof(OfficerMoveCommand) }, names);
    }

    [Fact]
    public void PCH_008_the_input_mapper_takes_no_ability_input()
    {
        var m = typeof(OfficerInputMapper).GetMethod(nameof(OfficerInputMapper.Map))!;
        var names = m.GetParameters().Select(p => p.Name).ToArray();
        Assert.Equal(new[] { "officer", "currentAim", "currentMove", "forward", "strafe", "lookDelta", "fire" }, names);
    }

    [Fact]
    public void DLV_014_officer_stats_come_from_the_stats_file_and_a_missing_value_is_an_error()
    {
        var s = Stats();
        Assert.True(s.OfficerSpeed > 0 && s.OfficerRifleRange > 0 && s.OfficerRifleDamage > 0 && s.OfficerRifleHitRadius > 0);
        var broken = Text("stats.json").Replace("\"rifleRange\"", "\"rifleRangeX\"");
        Assert.Throws<DataLoadException>(() => UnitStats.Load(broken));
    }
}
