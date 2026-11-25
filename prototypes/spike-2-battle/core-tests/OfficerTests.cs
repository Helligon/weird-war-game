using System.Reflection;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

public class OfficerTests
{
    private static string DataDir
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "data", "map.json"))) dir = dir.Parent;
            return Path.Combine(dir!.FullName, "data");
        }
    }

    private static string Text(string file) => File.ReadAllText(Path.Combine(DataDir, file));
    private static UnitStats Stats() => UnitStats.Load(Text("stats.json"));

    private static BattleState NewBattle(ulong seed = 1) =>
        BattleState.SetUp(BattleMapLoader.Load(Text("map.json")), Stats(), BattleParameters.Load(Text("parameters.json")), seed);

    // Walks the Officer east until the enemy is inside rifle range (the spawn points are further apart than the range).
    private static void CloseIn(BattleState b)
    {
        Assert.True(b.ApplyOfficerCommand(new OfficerMoveCommand(0, 1, 0, 10)).IsAccepted);
    }

    // Aims the Officer at the enemy Section.
    private static void FaceEnemy(BattleState b)
    {
        var enemy = b.Find(BattleState.EnemySectionId)!;
        var o = b.Officers[0];
        var dx = enemy.Position.X - o.Position.X;
        var dy = enemy.Position.Y - o.Position.Y;
        Assert.True(b.ApplyOfficerCommand(new OfficerAimCommand(0, Math.Atan2(dy, dx))).IsAccepted);
    }

    [Fact]
    public void PCH_001_move_command_walks_the_officer_at_stats_speed()
    {
        var b = NewBattle();
        var x0 = b.Officers[0].Position.X;
        var r = b.ApplyOfficerCommand(new OfficerMoveCommand(0, 1, 0, 2));
        Assert.True(r.IsAccepted);
        Assert.Equal(x0 + Stats().OfficerSpeed * 2, b.Officers[0].Position.X, 6);
    }

    [Fact]
    public void PCH_001_move_direction_is_capped_so_diagonals_are_not_faster()
    {
        var b = NewBattle();
        var p0 = b.Officers[0].Position;
        b.ApplyOfficerCommand(new OfficerMoveCommand(0, 5, 5, 1));
        var p1 = b.Officers[0].Position;
        var dist = Math.Sqrt(Math.Pow(p1.X - p0.X, 2) + Math.Pow(p1.Y - p0.Y, 2));
        Assert.Equal(Stats().OfficerSpeed, dist, 6);
    }

    [Fact]
    public void PCH_001_officer_cannot_leave_the_map()
    {
        var b = NewBattle();
        b.ApplyOfficerCommand(new OfficerMoveCommand(0, -1, 0, 1000));
        Assert.Equal(0, b.Officers[0].Position.X);
    }

    [Fact]
    public void PCH_001_aim_command_turns_the_officer()
    {
        var b = NewBattle();
        Assert.True(b.ApplyOfficerCommand(new OfficerAimCommand(0, 1.25)).IsAccepted);
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
        var r = b.ApplyOfficerCommand(new OfficerFireCommand(0));
        Assert.True(r.IsAccepted);
        Assert.True(r.Shot!.Value.Hit);
        Assert.Equal(BattleState.EnemySectionId, r.Shot.Value.Target);
        Assert.Equal(Stats().OfficerRifleDamage, r.Shot.Value.Damage);
        Assert.True(enemy.UnitCount < unitsBefore);
    }

    [Fact]
    public void PCH_001_rifle_misses_when_aimed_away_and_damages_nothing()
    {
        var b = NewBattle();
        CloseIn(b);
        b.ApplyOfficerCommand(new OfficerAimCommand(0, Math.PI));
        var enemy = b.Find(BattleState.EnemySectionId)!;
        var health = enemy.Health;
        var r = b.ApplyOfficerCommand(new OfficerFireCommand(0));
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
        FaceEnemy(b);
        Assert.False(b.ApplyOfficerCommand(new OfficerFireCommand(0)).Shot!.Value.Hit);
    }

    [Fact]
    public void PCH_001_rifle_is_deterministic_for_the_same_commands()
    {
        int Run()
        {
            var b = NewBattle(7);
            CloseIn(b);
            FaceEnemy(b);
            b.ApplyOfficerCommand(new OfficerFireCommand(0));
            b.ApplyOfficerCommand(new OfficerFireCommand(0));
            return b.Find(BattleState.EnemySectionId)!.Health;
        }
        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void PCH_009_input_for_move_aim_and_fire_becomes_one_command_each()
    {
        var commands = OfficerInputMapper.Map(0, 0, forward: 1, strafe: 0, lookDelta: 0.1, fire: true, seconds: 0.02);
        Assert.Collection(commands,
            c => Assert.IsType<OfficerAimCommand>(c),
            c => Assert.IsType<OfficerMoveCommand>(c),
            c => Assert.IsType<OfficerFireCommand>(c));
    }

    [Fact]
    public void PCH_009_no_input_makes_no_commands()
    {
        Assert.Empty(OfficerInputMapper.Map(0, 0, 0, 0, 0, false, 0.02));
    }

    [Fact]
    public void PCH_009_forward_moves_along_the_aim_and_strafe_moves_to_its_right()
    {
        var north = Math.PI / 2;
        var fwd = (OfficerMoveCommand)OfficerInputMapper.Map(0, north, 1, 0, 0, false, 1).Single();
        Assert.Equal(0, fwd.DirectionX, 9);
        Assert.Equal(1, fwd.DirectionY, 9);
        var right = (OfficerMoveCommand)OfficerInputMapper.Map(0, north, 0, 1, 0, false, 1).Single();
        Assert.Equal(1, right.DirectionX, 9);
        Assert.Equal(0, right.DirectionY, 9);
    }

    [Fact]
    public void PCH_009_move_aim_and_fire_round_trip_through_json()
    {
        var commands = new IBattleCommand[]
        {
            new OfficerMoveCommand(0, 0.70710678118654757, -0.1, 0.016666666666666666),
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
        var c = new OfficerMoveCommand(0, 1, 0, 1);
        Assert.Equal(c, CommandSerialiser.Deserialise(CommandSerialiser.Serialise(c)));
    }

    [Fact]
    public void PCH_009_accepted_commands_are_recorded_in_the_command_log_in_order()
    {
        var b = NewBattle();
        var aim = new OfficerAimCommand(0, 0.5);
        var move = new OfficerMoveCommand(0, 1, 0, 0.1);
        var fire = new OfficerFireCommand(0);
        b.ApplyOfficerCommand(aim);
        b.ApplyOfficerCommand(move);
        b.ApplyOfficerCommand(fire);
        Assert.Equal(new IBattleCommand[] { aim, move, fire }, b.CommandLog);
    }

    [Fact]
    public void PCH_009_rejected_commands_change_nothing_and_are_not_logged()
    {
        var b = NewBattle();
        var p = b.Officers[0].Position;
        Assert.False(b.ApplyOfficerCommand(new OfficerMoveCommand(0, 1, 0, 0)).IsAccepted);
        Assert.False(b.ApplyOfficerCommand(new OfficerMoveCommand(0, double.NaN, 0, 1)).IsAccepted);
        Assert.False(b.ApplyOfficerCommand(new OfficerAimCommand(0, double.PositiveInfinity)).IsAccepted);
        Assert.False(b.ApplyOfficerCommand(new OfficerFireCommand(5)).IsAccepted);
        Assert.Equal(p.X, b.Officers[0].Position.X);
        Assert.Empty(b.CommandLog);
    }

    [Fact]
    public void DLV_013_officer_state_has_no_public_setters()
    {
        foreach (var name in new[] { nameof(Officer.Position), nameof(Officer.AimRadians) })
        {
            var setter = typeof(Officer).GetProperty(name)!.SetMethod;
            Assert.True(setter == null || !setter.IsPublic, name + " must not be settable from outside the core.");
        }
    }

    [Fact]
    public void DLV_013_replaying_the_logged_commands_gives_the_same_battle()
    {
        var a = NewBattle();
        CloseIn(a);
        FaceEnemy(a);
        a.ApplyOfficerCommand(new OfficerMoveCommand(0, 1, 0.2, 0.5));
        a.ApplyOfficerCommand(new OfficerFireCommand(0));
        var b = NewBattle();
        foreach (var c in a.CommandLog) b.ApplyOfficerCommand(CommandSerialiser.Deserialise(CommandSerialiser.Serialise(c)));
        Assert.Equal(a.Officers[0].Position.X, b.Officers[0].Position.X);
        Assert.Equal(a.Officers[0].Position.Y, b.Officers[0].Position.Y);
        Assert.Equal(a.Find(BattleState.EnemySectionId)!.Health, b.Find(BattleState.EnemySectionId)!.Health);
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
        var (x, y, h) = (mine.Position.X, mine.Position.Y, mine.Health);
        CloseIn(b);
        FaceEnemy(b);
        b.ApplyOfficerCommand(new OfficerMoveCommand(0, 0, 1, 3));
        b.ApplyOfficerCommand(new OfficerFireCommand(0));
        Assert.Equal((x, y, h), (mine.Position.X, mine.Position.Y, mine.Health));
    }

    [Fact]
    public void PCH_004_a_section_command_is_not_an_officer_command()
    {
        var b = NewBattle();
        var r = b.ApplyOfficerCommand(new StopCommand(BattleState.PlayerSectionId));
        Assert.False(r.IsAccepted);
    }

    [Fact]
    public void PCH_004_the_input_mapper_only_makes_officer_commands()
    {
        var all = OfficerInputMapper.Map(0, 0.3, 1, 1, 0.2, true, 0.02);
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
        Assert.Equal(new[] { "officer", "currentAim", "forward", "strafe", "lookDelta", "fire", "seconds" }, names);
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
