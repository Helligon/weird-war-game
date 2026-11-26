using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

/// <summary>SP2-42: the Officer Protects the nearest Section while under AI, the player gets it back, and the tactical-only rules when it dies.</summary>
public class OfficerAiTests
{
    private const string Stats =
        "{ \"riflemen\": { \"speed\": 2, \"P-HIE-01\": 8, \"P-HIE-02\": 30, \"range\": 30, \"damage\": 1, \"accuracy\": 0.6, \"volleyInterval\": 2 },"
        + " \"officer\": { \"health\": 10, \"speed\": 4, \"rifleRange\": 80, \"rifleDamage\": 3, \"rifleHitRadius\": 3, \"protectDistance\": 6, \"fireInterval\": 2 } }";
    private const double ProtectDistance = 6;

    private static BattleState Battle(ulong seed = 1) =>
        BattleState.SetUp(new BattleMap(200, 60, new List<CoverObject>()), UnitStats.Load(Stats),
            BattleParameters.Load("{\"P-CMD-01\": 2, \"P-DAI-01\": 25, \"P-CMD-04\": 3, \"P-CMD-03\": 0.5}"), seed);

    private static Section Mine(BattleState b) => b.Find(BattleState.PlayerSectionId)!;
    private static Section Foe(BattleState b) => b.Find(BattleState.EnemySectionId)!;
    private static void Steps(BattleState b, int n) { for (var i = 0; i < n; i++) b.Step(); }
    private static void Accept(BattleState b, IBattleCommand c) { var r = b.ApplyCommand(c); Assert.True(r.IsAccepted, r.Reason); }
    private static void AiOn(BattleState b) => Accept(b, new OfficerControlCommand(0, true));
    private static double Dist(MapPoint a, MapPoint c) => Math.Sqrt((a.X - c.X) * (a.X - c.X) + (a.Y - c.Y) * (a.Y - c.Y));

    private static Section Add(BattleState b, int id, Side side, double x, double y)
    {
        var s = new Section(new SectionId(id), side, UnitType.Riflemen, b.Stats.RiflemenMaxUnits, b.Stats.RiflemenHealthPerUnit, new MapPoint(x, y));
        b.Add(s);
        return s;
    }

    // The enemy holds and its volleys never come, so only the Officer's behaviour is being looked at.
    private static void QuietEnemy(BattleState b) { Foe(b).IsHolding = true; Foe(b).VolleyCooldown = 1e9; }

    // ---- DAI-005, CMD-010: Protects the nearest friendly Section ----

    [Fact]
    public void DAI_005_an_officer_not_under_ai_does_nothing_by_itself()
    {
        var b = Battle(); QuietEnemy(b);
        Mine(b).IsHolding = true;
        var start = b.Officers[0].Position;
        Steps(b, 100);
        Assert.Equal(start, b.Officers[0].Position);
        Assert.False(b.Officers[0].UnderAi);
    }

    [Fact]
    public void DAI_005_an_officer_under_ai_walks_to_the_nearest_friendly_section_and_stands_in_front_of_it()
    {
        var b = Battle(); QuietEnemy(b);
        Mine(b).IsHolding = true;
        b.Officers[0].Position = new MapPoint(Mine(b).Position.X - 40, Mine(b).Position.Y + 20);
        AiOn(b);
        Steps(b, 300);
        var o = b.Officers[0];
        var s = Mine(b);
        Assert.InRange(Dist(o.Position, s.Position), ProtectDistance - 0.5, ProtectDistance + 0.5);
        Assert.True(o.Position.X > s.Position.X, "in front of the Section, on the enemy's side");
    }

    [Fact]
    public void DAI_005_the_officer_protects_the_nearer_of_two_friendly_sections()
    {
        var b = Battle(); QuietEnemy(b);
        Mine(b).IsHolding = true;
        var other = Add(b, 5, Side.Player, 120, 40);
        other.IsHolding = true;
        b.Officers[0].Position = new MapPoint(110, 40);
        AiOn(b);
        Steps(b, 200);
        Assert.True(Dist(b.Officers[0].Position, other.Position) <= ProtectDistance + 0.5);
        Assert.True(Dist(b.Officers[0].Position, Mine(b).Position) > 50);
    }

    [Fact]
    public void CMD_010_the_officer_stays_with_its_section_while_the_section_advances()
    {
        var b = Battle();
        Foe(b).Position2(new MapPoint(150, 30));
        AiOn(b);
        var worst = 0.0;
        for (var i = 0; i < 400; i++)
        {
            b.Step();
            if (i > 20) worst = Math.Max(worst, Dist(b.Officers[0].Position, Mine(b).Position));
        }
        Assert.True(Mine(b).Position.X > 40, "the Section did advance");
        Assert.True(worst <= ProtectDistance + 1.0, "never farther from its Section than the protect distance, was " + worst);
    }

    [Fact]
    public void CMD_010_the_officer_engages_the_nearest_enemy_it_can_reach_and_not_one_out_of_reach()
    {
        var b = Battle(); QuietEnemy(b);
        Mine(b).IsHolding = true;
        Foe(b).Position2(new MapPoint(Mine(b).Position.X + 150, 30)); // out of rifle reach (80)
        var near = Add(b, 7, Side.Enemy, Mine(b).Position.X + 60, Mine(b).Position.Y);
        near.IsHolding = true; near.VolleyCooldown = 1e9;
        AiOn(b);
        Steps(b, 100);
        Assert.True(near.Health < near.MaxUnits * near.HealthPerUnit, "the reachable enemy was shot at");
        Assert.Equal(Foe(b).MaxUnits * Foe(b).HealthPerUnit, Foe(b).Health);
    }

    [Fact]
    public void CMD_010_the_officer_does_not_leave_its_section_to_chase_an_enemy()
    {
        var b = Battle(); QuietEnemy(b);
        Mine(b).IsHolding = true;
        Foe(b).Position2(new MapPoint(Mine(b).Position.X + 70, 30));
        AiOn(b);
        Steps(b, 200);
        Assert.True(Dist(b.Officers[0].Position, Mine(b).Position) <= ProtectDistance + 0.5);
    }

    [Fact]
    public void DAI_005_with_no_friendly_section_left_the_officer_stands_still()
    {
        var b = Battle(); QuietEnemy(b);
        Mine(b).TakeDamage(100000);
        AiOn(b);
        var p = b.Officers[0].Position;
        Steps(b, 50);
        Assert.Equal(p, b.Officers[0].Position);
    }

    // ---- CMD-004: the player gets the Officer back ----

    [Fact]
    public void CMD_004_resuming_in_the_tactical_view_hands_the_officer_to_the_ai_and_leaving_hands_it_back()
    {
        var b = Battle();
        var v = new TacticalView(b);
        v.Enter();
        Assert.False(b.Officers[0].UnderAi); // paused: the player's Officer just stands
        v.SetRunning(true);
        Assert.True(b.Officers[0].UnderAi);
        Steps(b, 20);
        Assert.True(v.Exit());
        Assert.False(b.Officers[0].UnderAi);
        Assert.Equal(new MapPoint(0, 0), b.Officers[0].MoveDirection);
        Assert.True(v.OfficerAcceptsInput);
        // The player controls it again: a move command is obeyed.
        var x = b.Officers[0].Position.X;
        Accept(b, new OfficerMoveCommand(0, -1, 0));
        Steps(b, 10);
        Assert.True(b.Officers[0].Position.X < x);
    }

    [Fact]
    public void CMD_004_the_handover_is_a_logged_command_not_a_view_flag()
    {
        var b = Battle();
        var v = new TacticalView(b);
        v.Enter();
        v.SetRunning(true);
        Steps(b, 5);
        v.Exit();
        var control = b.CommandLog.Select(l => l.Command).OfType<OfficerControlCommand>().ToList();
        Assert.Equal(new[] { true, false }, control.Select(c => c.UnderAi).ToArray());
        Assert.Equal(new[] { 0, 5 }, b.CommandLog.Select(l => l.Step).ToArray());
    }

    [Fact]
    public void CMD_004_an_ai_handover_command_is_not_a_player_officer_ability_PCH_008()
    {
        Assert.False(typeof(OfficerCommand).IsAssignableFrom(typeof(OfficerControlCommand)));
        var b = Battle();
        Assert.False(b.ApplyCommand(new OfficerControlCommand(3, true)).IsAccepted);
        Assert.Empty(b.CommandLog);
    }

    // ---- TEC-008: replay reproduces a battle where the Officer was AI controlled for some steps ----

    private static string Snapshot(BattleState b)
    {
        var parts = new List<string> { "step " + b.StepCount };
        foreach (var s in b.Sections) parts.Add(s.Id + " " + s.Health + " " + s.Position.X.ToString("R") + "," + s.Position.Y.ToString("R"));
        foreach (var o in b.Officers) parts.Add("officer " + o.Health + " " + o.Position.X.ToString("R") + "," + o.Position.Y.ToString("R")
            + " aim " + o.AimRadians.ToString("R") + " ai " + o.UnderAi);
        return string.Join("|", parts);
    }

    // The same starting layout every time (the enemy 50 m ahead), as a replay needs.
    private static BattleState Close(ulong seed)
    {
        var b = Battle(seed);
        Foe(b).Position2(new MapPoint(Mine(b).Position.X + 50, 30));
        return b;
    }

    [Fact]
    public void TEC_008_replay_reproduces_a_battle_in_which_the_officer_was_ai_controlled_for_some_steps()
    {
        var original = Close(9);
        var v = new TacticalView(original);
        Steps(original, 30);
        Accept(original, new OfficerMoveCommand(0, 1, 0));
        Steps(original, 20);
        v.Enter();                 // the player opens the tactical view
        v.Select(BattleState.PlayerSectionId);
        v.SetRunning(true);        // the battle runs, so the Officer is under AI
        Steps(original, 150);
        v.Exit();                  // the player takes the Officer back
        Steps(original, 40);
        var steps = original.StepCount;

        Assert.Contains(original.CommandLog, l => l.Command is OfficerControlCommand c && c.UnderAi);
        Assert.NotEqual(Snapshot(Close(9)), Snapshot(original));
        Assert.True(Foe(original).Health < Foe(original).MaxUnits * Foe(original).HealthPerUnit
            || Mine(original).Health < Mine(original).MaxUnits * Mine(original).HealthPerUnit, "the battle did something");

        var replayed = Close(9);
        replayed.Replay(original.CommandLog, steps);
        Assert.Equal(Snapshot(original), Snapshot(replayed));
        Assert.Equal(original.CommandLog.Count, replayed.CommandLog.Count);

        // Without the logged hand-over the replay is a different battle: the handover really is what drives the AI.
        var without = Close(9);
        without.Replay(original.CommandLog.Where(l => !(l.Command is OfficerControlCommand)).ToList(), steps);
        Assert.NotEqual(Snapshot(original), Snapshot(without));
    }

    [Fact]
    public void TEC_008_replay_of_the_serialised_log_reproduces_the_ai_battle()
    {
        var original = Battle(seed: 4);
        var v = new TacticalView(original);
        v.Enter(); v.SetRunning(true);
        Steps(original, 120);
        var text = original.CommandLog.Select(l => (l.Step, CommandSerialiser.Serialise(l.Command))).ToList();
        var back = text.Select(t => new LoggedCommand(t.Step, CommandSerialiser.Deserialise(t.Item2))).ToList();
        var replayed = Battle(seed: 4);
        replayed.Replay(back, original.StepCount);
        Assert.Equal(Snapshot(original), Snapshot(replayed));
        Assert.Contains("officerControl", text[0].Item2);
    }

    [Fact]
    public void TEC_008_the_ai_does_not_read_the_view_so_a_replay_needs_no_view()
    {
        var b = Battle();
        AiOn(b);
        Steps(b, 50);
        var moved = b.Officers[0].Position;
        var c = Battle();
        c.Replay(b.CommandLog, 50);               // no TacticalView exists here at all
        Assert.Equal(moved, c.Officers[0].Position);
    }

    [Fact]
    public void TEC_008_the_core_files_do_not_mention_the_view_in_the_ai()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "core"))) dir = dir.Parent;
        var ai = File.ReadAllText(Path.Combine(dir!.FullName, "core", "OfficerAi.cs"));
        Assert.DoesNotContain("TacticalView", ai.Replace("<see cref=\"TacticalView", ""));
        Assert.DoesNotContain("OfficerUnderAi", ai);
    }

    [Fact]
    public void DLV_013_the_control_command_serialises_and_round_trips()
    {
        var back = CommandSerialiser.Deserialise(CommandSerialiser.Serialise(new OfficerControlCommand(0, true)));
        Assert.Equal(new OfficerControlCommand(0, true), back);
        Assert.NotEqual(new OfficerControlCommand(0, false), back);
        Assert.Throws<CommandFormatException>(() => CommandSerialiser.Deserialise("{\"type\":\"officerControl\",\"officer\":0}"));
    }

    // ---- PCH-006: the officer dies ----

    [Fact]
    public void PCH_006_when_the_officer_dies_in_third_person_the_game_switches_to_the_tactical_view_paused()
    {
        var b = Battle();
        var v = new TacticalView(b);
        v.Enter(); v.Exit();        // third person, battle running
        Assert.False(v.Active);
        Assert.True(v.Running);
        Assert.False(v.CheckOfficerLost()); // alive: nothing happens

        b.Officers[0].TakeDamage(1000);
        Assert.True(v.CheckOfficerLost());
        Assert.True(v.Active);
        Assert.False(v.Running);              // CMD-002: paused
        Assert.False(v.OfficerAcceptsInput);
        Assert.False(v.CheckOfficerLost());   // only once
    }

    [Fact]
    public void PCH_006_with_no_other_officer_or_hero_left_the_player_cannot_return_to_third_person()
    {
        var b = Battle();
        var v = new TacticalView(b);
        v.Enter();
        b.Officers[0].TakeDamage(1000);
        Assert.False(v.CanReturnToThirdPerson);
        Assert.False(v.Exit());
        Assert.True(v.Active);
        Assert.False(v.OfficerAcceptsInput);
        Assert.Contains("tactical view only", v.Message);
        v.SetRunning(true);                   // the battle can still run inside the view
        Assert.True(v.Active);
        Assert.False(v.Exit());
    }

    [Fact]
    public void PCH_006_another_living_officer_keeps_third_person_possible()
    {
        var b = Battle();
        b.AddOfficer(new Officer(Side.Player, 10, new MapPoint(20, 20)));
        var v = new TacticalView(b);
        v.Enter();
        b.Officers[0].TakeDamage(1000);
        Assert.True(v.CanReturnToThirdPerson);
        Assert.True(v.Exit());
        Assert.False(v.CheckOfficerLost());
    }

    [Fact]
    public void PCH_006_an_enemy_section_that_kills_the_officer_triggers_the_switch()
    {
        var b = Battle();
        var v = new TacticalView(b);
        v.Enter(); v.Exit();
        b.Officers[0].Position = new MapPoint(Foe(b).Position.X - 5, Foe(b).Position.Y);
        for (var i = 0; i < 600 && !b.Officers[0].IsDestroyed; i++) b.Step();
        Assert.True(b.Officers[0].IsDestroyed);
        Assert.True(v.CheckOfficerLost());
        Assert.False(v.Running);
    }

    [Fact]
    public void PCH_006_a_dead_officer_cannot_be_commanded_or_handed_to_the_ai()
    {
        var b = Battle();
        b.Officers[0].TakeDamage(1000);
        Assert.False(b.ApplyCommand(new OfficerControlCommand(0, true)).IsAccepted);
        Assert.False(b.ApplyCommand(new OfficerMoveCommand(0, 1, 0)).IsAccepted);
        var v = new TacticalView(b);
        v.Enter(); v.SetRunning(true);
        Assert.Empty(b.CommandLog);
    }

    [Fact]
    public void DEC_094_the_section_fights_on_with_no_morale_penalty_after_the_officer_dies()
    {
        var b = Battle(seed: 3);
        var chanceBefore = b.HitChance(false);
        Foe(b).Position2(new MapPoint(Mine(b).Position.X + 20, Mine(b).Position.Y));
        b.Officers[0].TakeDamage(1000);
        var foeBefore = Foe(b).Health;
        Steps(b, 200);
        Assert.Equal(chanceBefore, b.HitChance(false));
        Assert.False(Mine(b).IsDestroyed && Foe(b).IsDestroyed);
        Assert.True(Foe(b).Health < foeBefore, "the Section kept fighting");
        // It still takes orders and carries them out.
        var fresh = Battle(seed: 3);
        fresh.Officers[0].TakeDamage(1000);
        Accept(fresh, new StopCommand(BattleState.PlayerSectionId));
        Assert.True(Mine(fresh).IsHolding);
    }

    // ---- DLV-014: data ----

    [Fact]
    public void DLV_014_the_protect_distances_come_from_the_stats_file_and_are_checked()
    {
        Assert.Equal(6, UnitStats.Load(Stats).OfficerProtectDistance);
        Assert.Equal(2, UnitStats.Load(Stats).OfficerFireInterval);
        Assert.Throws<DataLoadException>(() => UnitStats.Load(Stats.Replace("\"protectDistance\"", "\"x\"")));
        Assert.Throws<DataLoadException>(() => UnitStats.Load(Stats.Replace("\"fireInterval\": 2", "\"fireInterval\": 0")));
    }
}

internal static class SectionTestExtensions
{
    // Tests place sections by hand; MoveTo is internal to the core.
    internal static void Position2(this Section s, MapPoint p) => s.MoveTo(p);
}
