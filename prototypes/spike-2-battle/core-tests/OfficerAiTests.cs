using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

/// <summary>SP2-42: the Officer Protects the nearest Section while under AI, the player gets it back, and the tactical-only rules when it dies.</summary>
public class OfficerAiTests
{
    private const string Stats =
        "{ \"riflemen\": { \"speed\": 2, \"P-HIE-01\": 8, \"P-HIE-02\": 30, \"range\": 30, \"damage\": 1, \"accuracy\": 0.6, \"volleyInterval\": 2 },"
        + " \"officer\": { \"health\": 10, \"speed\": 4, \"rifleRange\": 80, \"rifleDamage\": 3, \"rifleHitRadius\": 3 } }";
    private const double ProtectDistance = 6;

    private static string Inv(double v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // The AI hits every time (P-DAI-04 = 1) unless a test says otherwise, so the geometry tests are about where it aims.
    private static string ParamsJson(double hit = 1.0, double interval = 2, double protect = 6) =>
        "{\"P-CMD-01\": 2, \"P-DAI-01\": 25, \"P-DAI-02\": " + Inv(protect) + ", \"P-DAI-03\": " + Inv(interval) + ", \"P-DAI-04\": " + Inv(hit) + ", \"P-CMD-04\": 3, \"P-CMD-03\": 0.5}";

    private static BattleState Battle(ulong seed = 1, double hit = 1.0, double interval = 2, double protect = 6) =>
        BattleState.SetUp(new BattleMap(200, 60, new List<CoverObject>()), UnitStats.Load(Stats),
            BattleParameters.Load(ParamsJson(hit, interval, protect)), seed);

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
    public void CMD_010_the_protect_distance_is_the_parameter_P_DAI_02_not_a_built_in_value()
    {
        foreach (var protect in new[] { 3.0, 10.0 })
        {
            var b = Battle(protect: protect); QuietEnemy(b);
            Mine(b).IsHolding = true;
            AiOn(b);
            Steps(b, 300);
            Assert.InRange(Dist(b.Officers[0].Position, Mine(b).Position), protect - 0.5, protect + 0.5);
        }
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
        // The far enemy is on another bearing, so a shot along the ray to it could not hit the near one by accident.
        Foe(b).Position2(new MapPoint(Mine(b).Position.X + 150, 55)); // out of rifle reach (80)
        var near = Add(b, 7, Side.Enemy, Mine(b).Position.X + 60, Mine(b).Position.Y);
        near.IsHolding = true; near.VolleyCooldown = 1e9;
        AiOn(b);
        Steps(b, 100);
        Assert.True(near.Health < near.MaxUnits * near.HealthPerUnit, "the reachable enemy was shot at");
        Assert.Equal(Foe(b).MaxUnits * Foe(b).HealthPerUnit, Foe(b).Health);
        Assert.Equal(0.0, b.Officers[0].AimRadians, 2); // aimed at the near enemy (due east), not at the far one (about 0.16 rad)
    }

    [Fact]
    public void CMD_010_with_the_only_enemy_out_of_reach_the_officer_takes_no_shot()
    {
        var b = Battle(); QuietEnemy(b);
        Mine(b).IsHolding = true;
        Foe(b).Position2(new MapPoint(Mine(b).Position.X + 150, 40)); // beyond the 80 m rifle reach
        var aim = b.Officers[0].AimRadians;
        AiOn(b);
        Steps(b, 100);
        Assert.Equal(Foe(b).MaxUnits * Foe(b).HealthPerUnit, Foe(b).Health);
        Assert.Equal(aim, b.Officers[0].AimRadians);
    }

    // ---- DAI-005, DLV-014: the AI's rate of fire is the parameter P-DAI-03 ----

    private static int DamageInTenSeconds(double interval, double hit = 1.0, ulong seed = 1)
    {
        var b = Battle(seed, hit, interval); QuietEnemy(b);
        Mine(b).IsHolding = true;
        Foe(b).Position2(new MapPoint(Mine(b).Position.X + 60, Mine(b).Position.Y));
        var before = Foe(b).Health;
        AiOn(b);
        Steps(b, 100);
        return before - Foe(b).Health;
    }

    [Fact]
    public void DAI_005_the_ai_officers_damage_over_time_follows_the_fire_interval()
    {
        var rifleDamage = UnitStats.Load(Stats).OfficerRifleDamage;
        // 10 s of battle time: shots at 0.1 s, then every interval. Interval 2 s -> 5 shots, 1 s -> 10 shots, 5 s -> 2 shots.
        Assert.Equal(5 * rifleDamage, DamageInTenSeconds(2));
        Assert.Equal(10 * rifleDamage, DamageInTenSeconds(1));
        Assert.Equal(2 * rifleDamage, DamageInTenSeconds(5));
    }

    // ---- DAI-005, DEC-102: the AI's shot rolls to hit with the battle's seeded random, at P-DAI-04 ----

    [Fact]
    public void DAI_005_with_the_ai_hit_chance_at_1_every_shot_hits()
    {
        var b = Battle(seed: 1, hit: 1.0); QuietEnemy(b);
        Mine(b).IsHolding = true;
        Foe(b).Position2(new MapPoint(Mine(b).Position.X + 60, Mine(b).Position.Y));
        AiOn(b);
        Steps(b, 400);
        Assert.True(b.Officers[0].AiShots >= 10);
        Assert.Equal(b.Officers[0].AiShots, b.Officers[0].AiHits);
    }

    [Fact]
    public void DAI_005_with_a_fixed_seed_and_the_ai_hit_chance_at_half_some_shots_miss_and_a_miss_does_no_damage()
    {
        var rifleDamage = UnitStats.Load(Stats).OfficerRifleDamage;
        foreach (var seed in new ulong[] { 1, 2, 3, 4, 5 })
        {
            var b = Battle(seed, hit: 0.5); QuietEnemy(b);
            Mine(b).IsHolding = true;
            Foe(b).Position2(new MapPoint(Mine(b).Position.X + 60, Mine(b).Position.Y));
            var before = Foe(b).Health;
            AiOn(b);
            Steps(b, 400);
            var o = b.Officers[0];
            Assert.Equal(before - o.AiHits * rifleDamage, Foe(b).Health);   // damage comes from hits only
            Assert.InRange(o.AiHits, 1, o.AiShots - 1);                      // some hit, some miss, whatever the seed
        }
    }

    [Fact]
    public void DAI_005_the_ai_hit_roll_is_repeatable_for_the_same_seed_and_differs_between_seeds()
    {
        int Hits(ulong seed)
        {
            var b = Battle(seed, hit: 0.5); QuietEnemy(b);
            Mine(b).IsHolding = true;
            Foe(b).Position2(new MapPoint(Mine(b).Position.X + 60, Mine(b).Position.Y));
            AiOn(b);
            Steps(b, 400);
            return b.Officers[0].AiHits;
        }
        Assert.Equal(Hits(3), Hits(3));
        Assert.Contains(new ulong[] { 1, 2, 4, 5, 6, 7 }, s => Hits(s) != Hits(3));
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
    public void CMD_004_the_core_rejects_player_officer_commands_while_the_officer_is_under_ai()
    {
        var b = Battle();
        AiOn(b);
        var logged = b.CommandLog.Count;
        Assert.False(b.ApplyCommand(new OfficerMoveCommand(0, 1, 0)).IsAccepted);
        Assert.False(b.ApplyCommand(new OfficerAimCommand(0, 1.0)).IsAccepted);
        Assert.False(b.ApplyCommand(new OfficerFireCommand(0)).IsAccepted);
        Assert.Equal(logged, b.CommandLog.Count);
        Accept(b, new OfficerControlCommand(0, false));
        Accept(b, new OfficerMoveCommand(0, 1, 0));
        Accept(b, new OfficerAimCommand(0, 1.0));
    }

    [Fact]
    public void CMD_004_the_view_hands_over_only_the_officer_the_player_controls()
    {
        var b = Battle();
        var second = new Officer(Side.Player, 10, new MapPoint(20, 20));
        b.AddOfficer(second);
        var v = new TacticalView(b);
        v.Enter(); v.SetRunning(true);
        Assert.True(b.Officers[0].UnderAi);
        Assert.False(second.UnderAi);
        Accept(b, new OfficerControlCommand(1, true)); // another Officer the AI already controls must not be handed back by the view
        v.Exit();
        Assert.False(b.Officers[0].UnderAi);
        Assert.True(second.UnderAi);
        // The only command for Officer 1 is the one the test itself sent: the view sent none.
        Assert.Single(b.CommandLog.Select(l => l.Command).OfType<OfficerControlCommand>().Where(c => c.Officer == 1));
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
        foreach (var o in b.Officers) parts.Add("officer shots " + o.AiShots + "/" + o.AiHits + " " + o.Health + " " + o.Position.X.ToString("R") + "," + o.Position.Y.ToString("R")
            + " aim " + o.AimRadians.ToString("R") + " ai " + o.UnderAi);
        return string.Join("|", parts);
    }

    // The same starting layout every time (the enemy 50 m ahead), as a replay needs.
    private static BattleState Close(ulong seed)
    {
        var b = Battle(seed, hit: 0.5);
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
        // The AI both hit and missed during the logged stretch (DEC-102), so the replay has to draw the same random numbers.
        Assert.InRange(original.Officers[0].AiHits, 1, original.Officers[0].AiShots - 1);
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
        Foe(b).Position2(new MapPoint(Mine(b).Position.X + 20, Mine(b).Position.Y));
        b.Officers[0].TakeDamage(1000);
        var foeBefore = Foe(b).Health;
        Steps(b, 200);
        Assert.True(Foe(b).Health < foeBefore, "the Section kept fighting");
        // It still takes orders and carries them out.
        var fresh = Battle(seed: 3);
        fresh.Officers[0].TakeDamage(1000);
        Accept(fresh, new StopCommand(BattleState.PlayerSectionId));
        Assert.True(Mine(fresh).IsHolding);
    }

    // ---- DLV-014: data ----

    [Fact]
    public void DLV_014_the_ai_officer_values_are_read_by_parameter_id_and_a_missing_or_bad_value_is_an_error()
    {
        var p = BattleParameters.Load(ParamsJson());
        Assert.Equal(6, p.OfficerProtectDistance);   // P-DAI-02
        Assert.Equal(2, p.OfficerFireInterval);      // P-DAI-03
        Assert.Equal(1, p.AiOfficerHitChance);       // P-DAI-04
        foreach (var id in new[] { "P-DAI-02", "P-DAI-03", "P-DAI-04" })
        {
            Assert.Throws<CommandFormatException>(() => BattleParameters.Load(ParamsJson().Replace("\"" + id + "\"", "\"x\"")));
        }
        Assert.Throws<CommandFormatException>(() => BattleParameters.Load(ParamsJson(hit: 1.5)));
        Assert.Throws<CommandFormatException>(() => BattleParameters.Load(ParamsJson(hit: -0.1)));
        Assert.Throws<CommandFormatException>(() => BattleParameters.Load(ParamsJson(interval: 0)));
    }

    [Fact]
    public void DLV_014_the_live_data_files_carry_the_three_ids_and_stats_json_no_longer_has_them()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "data", "map.json"))) dir = dir.Parent;
        var p = BattleParameters.Load(File.ReadAllText(Path.Combine(dir!.FullName, "data", "parameters.json")));
        Assert.Equal(6, p.OfficerProtectDistance);
        Assert.Equal(2, p.OfficerFireInterval);
        Assert.Equal(0.5, p.AiOfficerHitChance);
        var stats = File.ReadAllText(Path.Combine(dir.FullName, "data", "stats.json"));
        Assert.DoesNotContain("protectDistance", stats);
        Assert.DoesNotContain("fireInterval", stats);
    }
}

internal static class SectionTestExtensions
{
    // Tests place sections by hand; MoveTo is internal to the core.
    internal static void Position2(this Section s, MapPoint p) => s.MoveTo(p);
}
