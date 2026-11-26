using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

/// <summary>SP2-40: Sections carry out Move, Stop, Clear waypoints, Target and Fall back. Fixed stats, parameters and map in the test.</summary>
public class SectionOrderTests
{
    private static string Stats(double accuracy = 0.6) =>
        "{ \"riflemen\": { \"speed\": 2, \"P-HIE-01\": 8, \"P-HIE-02\": 30, \"range\": 30, \"damage\": 1, \"accuracy\": " + accuracy.ToString(System.Globalization.CultureInfo.InvariantCulture)
        + ", \"volleyInterval\": 2 },"
        + " \"officer\": { \"health\": 10, \"speed\": 4, \"rifleRange\": 80, \"rifleDamage\": 3, \"rifleHitRadius\": 3, \"protectDistance\": 6, \"fireInterval\": 2 } }";

    private static BattleMap OpenMap() => new BattleMap(100, 60, new List<CoverObject>());
    private static BattleMap CoverMap() => new BattleMap(100, 60, new List<CoverObject> { new CoverObject("ruin", new MapPoint(30, 30), 4) });

    private static BattleState Battle(BattleMap? map = null, double adjacency = 3, double penalty = 0.5, double accuracy = 0.6, ulong seed = 1) =>
        BattleState.SetUp(map ?? OpenMap(), UnitStats.Load(Stats(accuracy)),
            BattleParameters.Load("{\"P-CMD-01\": 2, \"P-DAI-01\": 25, \"P-CMD-04\": " + adjacency.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", \"P-CMD-03\": " + penalty.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}"), seed);

    private static Section Player(BattleState b) => b.Find(BattleState.PlayerSectionId)!;
    private static Section Enemy(BattleState b) => b.Find(BattleState.EnemySectionId)!;
    private static SectionId P => BattleState.PlayerSectionId;
    private static SectionId E => BattleState.EnemySectionId;

    private static Section AddSection(BattleState b, int id, Side side, double x, double y)
    {
        var s = new Section(new SectionId(id), side, UnitType.Riflemen, b.Stats.RiflemenMaxUnits, b.Stats.RiflemenHealthPerUnit, new MapPoint(x, y));
        b.Add(s);
        return s;
    }

    private static void Steps(BattleState b, int n) { for (var i = 0; i < n; i++) b.Step(); }
    private static void Accept(BattleState b, IBattleCommand c) { var r = b.ApplyCommand(c); Assert.True(r.IsAccepted, r.Reason); }
    private static Waypoint W(int x, int y) => new Waypoint(x, y);
    private static MoveCommand Move(SectionId s, params Waypoint[] w) => new MoveCommand(s, w);

    // Park the enemy far away and out of the way so default behaviour does not fire during order tests.
    private static void QuietEnemy(BattleState b) { Enemy(b).IsHolding = true; }

    // Runs until the predicate holds, at most `max` steps. Returns the steps taken.
    private static int RunUntil(BattleState b, Func<bool> done, int max = 2000)
    {
        var n = 0;
        while (!done() && n < max) { b.Step(); n++; }
        return n;
    }

    private static bool At(Section s, double x, double y) => s.Position.X == x && s.Position.Y == y;

    [Fact]
    public void CMD_005_a_move_with_two_waypoints_goes_to_the_first_then_the_second()
    {
        var b = Battle(); QuietEnemy(b);
        Accept(b, Move(P, W(10, 30), W(10, 40)));

        var firstAt = RunUntil(b, () => At(Player(b), 10, 30));
        Assert.True(At(Player(b), 10, 30));
        Assert.Single(Player(b).Waypoints, w => w.Y == 40); // the second is still queued
        RunUntil(b, () => At(Player(b), 10, 40));

        Assert.True(At(Player(b), 10, 40));
        // 4 m then 10 m at 2 m/s in 0.1 s steps: the first is reached before the second could be.
        Assert.Equal(20, firstAt);
    }

    [Fact]
    public void CMD_005_an_empty_move_is_rejected_and_not_logged()
    {
        var b = Battle();
        var r = b.ApplyCommand(Move(P));
        Assert.False(r.IsAccepted);
        Assert.Empty(b.CommandLog);
    }

    [Theory]
    [InlineData(3.0, true)]
    [InlineData(0.5, false)]
    public void CMD_005_a_waypoint_next_to_cover_takes_that_cover_and_the_distance_comes_from_data(double adjacency, bool takesCover)
    {
        // The ruin is at (30,30) with radius 4. The waypoint (25,30) is 1 m from its edge.
        var b = Battle(CoverMap(), adjacency); QuietEnemy(b);
        Accept(b, Move(P, W(25, 30)));
        Accept(b, new StopCommand(P)); // hold after the waypoint, so default behaviour does not walk it out again

        RunUntil(b, () => Player(b).IsHolding);
        Steps(b, 5);

        Assert.Equal(takesCover, b.Map.CoverAt(Player(b).Position) != null);
        if (!takesCover) Assert.True(At(Player(b), 25, 30));
    }

    [Fact]
    public void CMD_005_a_waypoint_far_from_cover_is_not_changed()
    {
        var b = Battle(CoverMap()); QuietEnemy(b);
        Accept(b, Move(P, W(12, 10)));
        RunUntil(b, () => Player(b).Waypoints.Count == 0);
        Assert.True(At(Player(b), 12, 10));
    }

    [Fact]
    public void CMD_020_a_second_move_adds_its_waypoint_after_the_existing_one_and_both_are_visited_in_order()
    {
        var b = Battle(); QuietEnemy(b);
        Accept(b, Move(P, W(10, 30)));
        Steps(b, 5);
        Accept(b, Move(P, W(10, 40)));
        Assert.Equal(new[] { new MapPoint(10, 30), new MapPoint(10, 40) }, Player(b).Waypoints.ToArray());

        RunUntil(b, () => At(Player(b), 10, 30));
        Assert.True(At(Player(b), 10, 30));
        RunUntil(b, () => At(Player(b), 10, 40));
        Assert.True(At(Player(b), 10, 40));
    }

    [Fact]
    public void CMD_020_a_move_that_would_go_over_P_CMD_01_is_rejected_and_the_queue_is_unchanged()
    {
        var b = Battle();
        Accept(b, Move(P, W(10, 30), W(10, 40)));
        var r = b.ApplyCommand(Move(P, W(10, 50)));
        Assert.False(r.IsAccepted);
        Assert.Equal(2, Player(b).Waypoints.Count);
        Assert.Single(b.CommandLog);
    }

    [Fact]
    public void CMD_005_a_waypoint_outside_the_map_is_rejected()
    {
        var b = Battle();
        Assert.False(b.ApplyCommand(Move(P, W(101, 30))).IsAccepted);
        Assert.False(b.ApplyCommand(Move(P, W(10, -1))).IsAccepted);
        Assert.Empty(Player(b).Waypoints);
    }

    [Fact]
    public void CMD_006_stop_after_two_waypoints_carries_out_both_then_holds_the_final_position()
    {
        var b = Battle(); QuietEnemy(b);
        Accept(b, Move(P, W(10, 30), W(10, 40)));
        Accept(b, new StopCommand(P));
        Assert.False(Player(b).IsHolding); // not yet

        RunUntil(b, () => Player(b).IsHolding);
        Assert.True(At(Player(b), 10, 40));
        Steps(b, 300); // the enemy holds too, so only the hold keeps it here; default behaviour would walk east
        Assert.True(At(Player(b), 10, 40));
    }

    [Fact]
    public void CMD_006_stop_with_no_waypoints_stops_the_section_on_the_spot()
    {
        var b = Battle(); QuietEnemy(b);
        var (x, y) = (Player(b).Position.X, Player(b).Position.Y);
        Accept(b, new StopCommand(P));
        Assert.True(Player(b).IsHolding);
        Steps(b, 100);
        Assert.True(At(Player(b), x, y));
    }

    [Fact]
    public void CMD_007_clear_waypoints_empties_the_queue_and_the_section_no_longer_walks_to_them()
    {
        var b = Battle(); QuietEnemy(b);
        Accept(b, Move(P, W(6, 55), W(6, 58)));
        Steps(b, 20);
        var y = Player(b).Position.Y;
        Assert.True(y > 30);

        Accept(b, new ClearWaypointsCommand(P));
        Assert.Empty(Player(b).Waypoints);
        Steps(b, 50);
        // Back on default behaviour (CMD-012 stand-in): it walks at the enemy (south-east), not on north to the cleared waypoints.
        Assert.True(Player(b).Position.Y < y);
        Assert.True(Player(b).Position.X > 6);
    }

    [Fact]
    public void CMD_007_clear_waypoints_keeps_a_pending_stop_so_the_section_holds_where_it_is()
    {
        var b = Battle(); QuietEnemy(b);
        Accept(b, Move(P, W(6, 55), W(6, 58)));
        Accept(b, new StopCommand(P));
        Steps(b, 20);
        Accept(b, new ClearWaypointsCommand(P));
        Assert.Empty(Player(b).Waypoints);
        var (x, y) = (Player(b).Position.X, Player(b).Position.Y);
        Steps(b, 50);
        Assert.True(At(Player(b), x, y)); // held, not walking at the enemy
        Assert.True(Player(b).IsHolding);
    }

    [Fact]
    public void CMD_007_clear_waypoints_with_no_pending_stop_returns_to_default_behaviour()
    {
        var b = Battle(); QuietEnemy(b);
        Accept(b, Move(P, W(6, 55)));
        Steps(b, 10);
        Accept(b, new ClearWaypointsCommand(P));
        Assert.False(Player(b).IsHolding);
        var x = Player(b).Position.X;
        Steps(b, 50);
        Assert.True(Player(b).Position.X > x);
    }

    [Fact]
    public void CMD_007_waypoints_stay_queued_when_no_clear_or_fall_back_is_given()
    {
        var b = Battle(); QuietEnemy(b);
        Accept(b, Move(P, W(10, 30), W(10, 40)));
        Accept(b, new StopCommand(P));
        Accept(b, new TargetCommand(P, E));
        Steps(b, 10);
        Assert.Equal(2, Player(b).Waypoints.Count);
    }

    [Fact]
    public void CMD_008_a_section_with_a_target_and_no_waypoints_attacks_that_section_even_when_another_is_nearer()
    {
        var b = Battle(); QuietEnemy(b);
        var decoy = AddSection(b, 3, Side.Enemy, 6, 50); // 20 m north: nearer than the ordered target below
        decoy.IsHolding = true;
        decoy.VolleyCooldown = 1000; // it never fires, so DAI-006 attacker preference cannot explain the result
        Enemy(b).MoveTo(new MapPoint(30, 30));          // 24 m east: in range
        Accept(b, new TargetCommand(P, E));

        Steps(b, 100);

        Assert.True(Enemy(b).Health < Enemy(b).MaxUnits * Enemy(b).HealthPerUnit);
        Assert.Equal(decoy.MaxUnits * decoy.HealthPerUnit, decoy.Health);
    }

    [Fact]
    public void CMD_008_a_section_with_a_target_out_of_range_closes_in_on_it()
    {
        var b = Battle(); QuietEnemy(b);
        var decoy = AddSection(b, 3, Side.Enemy, 6, 55); decoy.IsHolding = true; // in range, so default behaviour would stay put
        Enemy(b).MoveTo(new MapPoint(50, 30));
        Accept(b, new TargetCommand(P, E));
        Steps(b, 100);
        Assert.True(Player(b).Position.X > 6);
        Assert.True(Enemy(b).Health < Enemy(b).MaxUnits * Enemy(b).HealthPerUnit);
    }

    [Fact]
    public void CMD_008_moving_takes_priority_over_the_target()
    {
        var b = Battle(); QuietEnemy(b);
        Enemy(b).MoveTo(new MapPoint(40, 30));
        Accept(b, Move(P, W(6, 40)));
        Accept(b, new TargetCommand(P, E));

        RunUntil(b, () => At(Player(b), 6, 40));
        Assert.True(At(Player(b), 6, 40)); // it went to the waypoint, not towards the target
        Steps(b, 50);
        Assert.True(Player(b).Position.X > 6); // then it attacks the target
    }

    [Fact]
    public void CMD_008_a_destroyed_target_ends_the_order()
    {
        var b = Battle(); QuietEnemy(b);
        Accept(b, new TargetCommand(P, E));
        Enemy(b).TakeDamage(10000);
        Steps(b, 2);
        Assert.Null(Player(b).OrderedTarget);
    }

    [Fact]
    public void CMD_008_a_target_must_exist_be_an_enemy_and_be_alive()
    {
        var b = Battle();
        var ally = AddSection(b, 3, Side.Player, 10, 10);
        Assert.False(b.ApplyCommand(new TargetCommand(P, new SectionId(99))).IsAccepted);
        Assert.False(b.ApplyCommand(new TargetCommand(P, ally.Id)).IsAccepted);
        Enemy(b).TakeDamage(10000);
        Assert.False(b.ApplyCommand(new TargetCommand(P, E)).IsAccepted);
        Assert.Empty(b.CommandLog);
    }

    [Fact]
    public void CMD_011_fall_back_clears_waypoints_moves_to_the_sides_own_edge_and_holds_there()
    {
        var b = Battle(); QuietEnemy(b);
        Player(b).MoveTo(new MapPoint(20, 30));
        Accept(b, Move(P, W(40, 30), W(40, 40)));
        Accept(b, new FallBackCommand(P));
        Assert.DoesNotContain(new MapPoint(40, 30), Player(b).Waypoints);
        Assert.DoesNotContain(new MapPoint(40, 40), Player(b).Waypoints);

        RunUntil(b, () => Player(b).IsHolding);
        Assert.True(At(Player(b), 0, 30)); // the Player side's own edge (BMP-004)
        Steps(b, 100);
        Assert.True(At(Player(b), 0, 30));
        Assert.False(Player(b).IsDestroyed);
        Assert.Empty(Player(b).Waypoints);
    }

    [Fact]
    public void CMD_011_an_enemy_section_falls_back_to_the_east_edge()
    {
        var b = Battle();
        Player(b).IsHolding = true;
        Enemy(b).MoveTo(new MapPoint(80, 20));
        Accept(b, new FallBackCommand(E));
        RunUntil(b, () => Enemy(b).IsHolding);
        Assert.True(At(Enemy(b), 100, 20));
    }

    [Fact]
    public void CMD_011_a_section_that_has_fallen_back_still_fires_at_enemies_in_range()
    {
        var b = Battle(); QuietEnemy(b);
        Player(b).MoveTo(new MapPoint(5, 30));
        Enemy(b).MoveTo(new MapPoint(25, 30));
        Accept(b, new FallBackCommand(P));
        Steps(b, 100);
        Assert.True(Enemy(b).Health < Enemy(b).MaxUnits * Enemy(b).HealthPerUnit);
    }

    [Fact]
    public void CMD_012_a_section_that_reaches_its_last_waypoint_without_a_stop_returns_to_default_behaviour()
    {
        var b = Battle(); QuietEnemy(b);
        Accept(b, Move(P, W(10, 30)));
        RunUntil(b, () => Player(b).Waypoints.Count == 0);
        Assert.False(Player(b).IsHolding);
        var x = Player(b).Position.X;
        Steps(b, 50);
        Assert.True(Player(b).Position.X > x); // DAI-001: walks at the enemy again
    }

    [Fact]
    public void CMD_016_a_command_affects_only_the_section_it_names()
    {
        var b = Battle(); QuietEnemy(b);
        var other = AddSection(b, 3, Side.Player, 6, 10);
        other.IsHolding = true;
        Accept(b, Move(P, W(10, 30)));
        Accept(b, new StopCommand(P));
        Accept(b, new TargetCommand(P, E));
        Assert.Empty(other.Waypoints);
        Assert.False(other.StopAtEnd);
        Assert.Null(other.OrderedTarget);
        Steps(b, 50);
        Assert.True(At(other, 6, 10));
        Accept(b, Move(other.Id, W(20, 10)));
        Assert.Single(other.Waypoints);
    }

    [Fact]
    public void CMD_016_a_command_naming_a_section_that_does_not_exist_is_rejected()
    {
        var b = Battle();
        Assert.False(b.ApplyCommand(new StopCommand(new SectionId(42))).IsAccepted);
        Assert.False(b.ApplyCommand(new FallBackCommand(new SectionId(42))).IsAccepted);
        Assert.Empty(b.CommandLog);
    }

    [Fact]
    public void HIE_007_one_order_moves_the_whole_section_because_units_have_no_positions_of_their_own()
    {
        var b = Battle(); QuietEnemy(b);
        var units = Player(b).UnitCount;
        Accept(b, Move(P, W(10, 30)));
        RunUntil(b, () => At(Player(b), 10, 30));
        Assert.Equal(units, Player(b).UnitCount);
        Assert.DoesNotContain(typeof(Section).GetProperties(), p => p.Name.Contains("Unit") && p.PropertyType == typeof(MapPoint));
    }

    [Fact]
    public void CMD_019_a_section_firing_on_the_move_uses_the_P_CMD_03_penalty()
    {
        // Penalty 1 means a moving shot cannot hit; accuracy 1 means a standing shot always hits.
        var b = Battle(penalty: 1.0, accuracy: 1.0); QuietEnemy(b);
        Player(b).MoveTo(new MapPoint(20, 30));
        Enemy(b).MoveTo(new MapPoint(40, 30));
        var full = Enemy(b).Health;
        Accept(b, Move(P, W(20, 40)));

        RunUntil(b, () => At(Player(b), 20, 40));
        Assert.Equal(full, Enemy(b).Health); // fired on the move, every shot missed
        Accept(b, new StopCommand(P));
        Steps(b, 30);
        Assert.True(Enemy(b).Health < full); // standing: it hits
    }

    [Fact]
    public void Stand_in_a_destroyed_section_drops_its_queued_commands()
    {
        var b = Battle();
        Accept(b, Move(P, W(10, 30), W(10, 40)));
        Accept(b, new StopCommand(P));
        Accept(b, new TargetCommand(P, E));
        Player(b).TakeDamage(10000);
        Assert.Empty(Player(b).Waypoints);
        Assert.False(Player(b).StopAtEnd);
        Assert.Null(Player(b).OrderedTarget);
        Assert.False(b.ApplyCommand(new StopCommand(P)).IsAccepted);
    }

    [Fact]
    public void CMD_005_a_section_already_in_a_cover_still_walks_to_a_waypoint_next_to_that_cover()
    {
        // Ruin at (30,30) r4. The Section starts inside it at (27,30); the waypoint (36,30) is 2 m past the far edge.
        var b = Battle(CoverMap()); QuietEnemy(b);
        Player(b).MoveTo(new MapPoint(27, 30));
        Accept(b, Move(P, W(36, 30)));
        Accept(b, new StopCommand(P));

        RunUntil(b, () => Player(b).IsHolding);
        Steps(b, 20);

        Assert.True(Player(b).Position.X >= 33 && Player(b).Position.X <= 36, "ended at " + Player(b).Position.X);
        Assert.NotNull(b.Map.CoverAt(Player(b).Position)); // it took the cover next to the waypoint on arrival
    }

    [Fact]
    public void CMD_005_cover_is_taken_at_a_waypoint_that_is_not_the_last()
    {
        // The first waypoint (25,30) is next to the ruin; the second is far away. The Section must step into the ruin first.
        var b = Battle(CoverMap()); QuietEnemy(b);
        Accept(b, Move(P, W(25, 30), W(25, 50)));

        var tookCover = false;
        for (var i = 0; i < 400 && Player(b).Waypoints.Count > 0 && !(tookCover = b.Map.CoverAt(Player(b).Position) != null); i++) b.Step();

        Assert.True(tookCover);
        Assert.Equal(2, Player(b).Waypoints.Count); // still on the first waypoint when it got into cover
        RunUntil(b, () => At(Player(b), 25, 50));
        Assert.True(At(Player(b), 25, 50)); // and then it carried on to the second
    }

    [Fact]
    public void CMD_012_a_move_given_to_a_holding_section_releases_the_hold_and_it_walks_the_new_waypoint()
    {
        var b = Battle(); QuietEnemy(b);
        Accept(b, new StopCommand(P));
        Assert.True(Player(b).IsHolding);
        Accept(b, Move(P, W(10, 30)));
        Assert.False(Player(b).IsHolding);
        RunUntil(b, () => At(Player(b), 10, 30));
        Assert.True(At(Player(b), 10, 30));
        var x = Player(b).Position.X;
        Steps(b, 30);
        Assert.True(Player(b).Position.X > x); // back on default behaviour: the hold did not come back
    }

    [Fact]
    public void CMD_006_a_move_given_after_a_pending_stop_makes_the_stop_no_longer_final()
    {
        var b = Battle(); QuietEnemy(b);
        Accept(b, Move(P, W(10, 30)));
        Accept(b, new StopCommand(P));
        Assert.True(Player(b).StopAtEnd);
        Accept(b, Move(P, W(10, 40)));
        Assert.False(Player(b).StopAtEnd);
        RunUntil(b, () => Player(b).Waypoints.Count == 0);
        Assert.False(Player(b).IsHolding);
    }

    [Fact]
    public void PCH_004_no_command_type_carries_a_unit_index_or_unit_id()
    {
        var commandTypes = typeof(IBattleCommand).Assembly.GetTypes().Where(t => typeof(IBattleCommand).IsAssignableFrom(t) && !t.IsInterface);
        Assert.NotEmpty(commandTypes);
        foreach (var t in commandTypes)
        {
            foreach (var p in t.GetProperties())
            {
                Assert.DoesNotContain("Unit", p.Name);
            }
        }
    }

    // ---- DLV-013, TEC-008: one apply path, one log, replay ----

    private static void Script(BattleState b, int steps)
    {
        for (var step = 0; step < steps; step++)
        {
            if (step == 0) Accept(b, Move(P, W(20, 30), W(20, 40)));
            if (step == 5) Accept(b, new TargetCommand(P, E));
            if (step == 30) Accept(b, new StopCommand(P));
            if (step == 40) Accept(b, new OfficerMoveCommand(0, 1, 0));
            if (step == 100) Accept(b, new FallBackCommand(E));
            if (step == 150) Accept(b, new ClearWaypointsCommand(E));
            b.Step();
        }
    }

    [Fact]
    public void TEC_008_replaying_a_log_with_section_and_officer_commands_reproduces_the_battle()
    {
        var a = Battle(CoverMap(), seed: 9);
        Script(a, 400);
        Assert.Equal(6, a.CommandLog.Count);

        var saved = a.CommandLog.Select(l => new LoggedCommand(l.Step, CommandSerialiser.Deserialise(CommandSerialiser.Serialise(l.Command)))).ToList();
        var c = Battle(CoverMap(), seed: 9);
        c.Replay(saved, 400);

        foreach (var id in new[] { P, E })
        {
            Assert.Equal(a.Find(id)!.Position.X, c.Find(id)!.Position.X);
            Assert.Equal(a.Find(id)!.Position.Y, c.Find(id)!.Position.Y);
            Assert.Equal(a.Find(id)!.Health, c.Find(id)!.Health);
            Assert.Equal(a.Find(id)!.IsHolding, c.Find(id)!.IsHolding);
        }
        Assert.Equal(a.Officers[0].Position.X, c.Officers[0].Position.X);
        Assert.Equal(a.CommandLog.Select(l => l.Step), c.CommandLog.Select(l => l.Step));
        Assert.True(Player(a).Position.X != 6); // the orders did something
    }

    [Fact]
    public void TEC_008_replay_throws_when_a_logged_section_command_is_rejected_now()
    {
        var b = Battle();
        var ex = Assert.Throws<InvalidOperationException>(() => b.Replay(new[] { new LoggedCommand(2, new StopCommand(new SectionId(9))) }, 5));
        Assert.Contains("step 2", ex.Message);
    }

    [Fact]
    public void TEC_008_replay_rejects_a_log_whose_steps_are_out_of_order()
    {
        var b = Battle();
        var log = new[]
        {
            new LoggedCommand(5, new StopCommand(P)),
            new LoggedCommand(3, new OfficerAimCommand(0, 1)),
        };
        var ex = Assert.Throws<InvalidOperationException>(() => b.Replay(log, 10));
        Assert.Contains("out of order", ex.Message);
        Assert.Equal(0, b.StepCount); // checked before anything ran
    }

    [Fact]
    public void DLV_013_rejected_commands_change_nothing_and_are_not_logged()
    {
        var b = Battle();
        Assert.False(b.ApplyCommand(Move(P)).IsAccepted);
        Assert.False(b.ApplyCommand(new TargetCommand(P, P)).IsAccepted);
        Assert.Empty(b.CommandLog);
        Assert.Empty(Player(b).Waypoints);
    }

    [Fact]
    public void DLV_013_there_is_one_apply_path_for_all_commands()
    {
        Assert.Null(typeof(BattleState).GetMethod("ApplyOfficerCommand"));
        Assert.NotNull(typeof(BattleState).GetMethod("ApplyCommand"));
    }

    // ---- DLV-014: data ----

    [Fact]
    public void DLV_014_the_cover_search_range_is_read_by_its_parameter_id_P_DAI_01()
    {
        Assert.Equal(25, Battle().Parameters.CoverSearchRange);
        Assert.Throws<CommandFormatException>(() => BattleParameters.Load("{\"P-CMD-01\": 2, \"P-CMD-03\": 0.5}"));
    }

    [Fact]
    public void DLV_014_the_cover_adjacency_distance_is_read_by_its_parameter_id_P_CMD_04()
    {
        Assert.Equal(3, Battle().Parameters.CoverAdjacencyDistance);
        Assert.Equal(7, Battle(adjacency: 7).Parameters.CoverAdjacencyDistance);
        Assert.Throws<CommandFormatException>(() => BattleParameters.Load("{\"P-CMD-01\": 2, \"P-CMD-03\": 0.5, \"P-DAI-01\": 25}"));
        Assert.Throws<CommandFormatException>(() => BattleParameters.Load("{\"P-CMD-01\": 2, \"P-CMD-03\": 0.5, \"P-DAI-01\": 25, \"P-CMD-04\": -1}"));
    }

    [Fact]
    public void DLV_014_the_live_data_files_load_with_the_new_keys()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "data", "map.json"))) dir = dir.Parent;
        var stats = UnitStats.Load(File.ReadAllText(Path.Combine(dir!.FullName, "data", "stats.json")));
        var parameters = BattleParameters.Load(File.ReadAllText(Path.Combine(dir.FullName, "data", "parameters.json")));
        Assert.True(parameters.CoverSearchRange > 0);
        Assert.True(parameters.CoverAdjacencyDistance >= 0);
    }
}
