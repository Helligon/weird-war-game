using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

/// <summary>SP2-41: the tactical view's rules (mode, pause, selection, waypoint cap, commands). Fixed stats, parameters and map.</summary>
public class TacticalViewTests
{
    private const string Stats =
        "{ \"riflemen\": { \"speed\": 2, \"P-HIE-01\": 8, \"P-HIE-02\": 30, \"range\": 30, \"damage\": 1, \"accuracy\": 0.6, \"volleyInterval\": 2, \"coverAdjacencyDistance\": 3 },"
        + " \"officer\": { \"health\": 10, \"speed\": 4, \"rifleRange\": 80, \"rifleDamage\": 3, \"rifleHitRadius\": 3 } }";

    private static BattleState Battle() =>
        BattleState.SetUp(new BattleMap(100, 60, new List<CoverObject>()), UnitStats.Load(Stats),
            BattleParameters.Load("{\"P-CMD-01\": 2, \"P-DAI-01\": 25, \"P-CMD-03\": 0.5}"), 1);

    private static SectionId P => BattleState.PlayerSectionId;
    private static SectionId E => BattleState.EnemySectionId;

    private static (BattleState, TacticalView) InView()
    {
        var b = Battle();
        var v = new TacticalView(b);
        v.Enter();
        return (b, v);
    }

    [Fact]
    public void CMD_001_section_commands_are_not_possible_outside_the_tactical_view()
    {
        var b = Battle();
        var v = new TacticalView(b);

        Assert.False(v.CanCommandSections);
        Assert.False(v.Select(P));
        Assert.False(v.Click(new MapPoint(50, 30)));
        Assert.False(v.Stop());
        Assert.False(v.FallBack());
        Assert.False(v.ClearWaypoints());
        Assert.False(v.Target(E));
        Assert.False(v.ConfirmMove());
        Assert.Empty(b.CommandLog);
    }

    [Fact]
    public void CMD_001_leaving_the_view_makes_section_commands_impossible_again()
    {
        var (b, v) = InView();
        Assert.True(v.CanCommandSections);
        Assert.True(v.Select(P));
        v.Exit();
        Assert.False(v.Stop());
        Assert.Empty(b.CommandLog);
    }

    [Fact]
    public void CMD_002_entering_pauses_and_leaving_resumes_without_creating_commands()
    {
        var b = Battle();
        var v = new TacticalView(b);
        v.SetRunning(true);

        v.Enter();
        Assert.False(v.Running);
        v.Exit();
        Assert.True(v.Running);

        Assert.Empty(b.CommandLog);
        Assert.Equal(0, b.StepCount);
    }

    [Fact]
    public void CMD_004_resuming_inside_the_view_puts_the_officer_under_ai_and_takes_no_player_input()
    {
        var (_, v) = InView();
        Assert.False(v.OfficerUnderAi);
        Assert.False(v.OfficerAcceptsInput);

        v.SetRunning(true);
        Assert.True(v.OfficerUnderAi);
        Assert.False(v.OfficerAcceptsInput);

        v.Exit();
        Assert.False(v.OfficerUnderAi);
        Assert.True(v.OfficerAcceptsInput);
    }

    [Fact]
    public void CMD_016_only_one_section_is_selected_at_a_time()
    {
        var (b, v) = InView();
        var second = new Section(new SectionId(3), Side.Player, UnitType.Riflemen, 8, 30, new MapPoint(20, 10));
        b.Add(second);

        Assert.True(v.Select(P));
        Assert.True(v.Select(second.Id));

        Assert.Equal(second.Id, v.Selected);
    }

    [Fact]
    public void CMD_016_an_enemy_section_cannot_be_selected()
    {
        var (_, v) = InView();
        Assert.False(v.Select(E));
        Assert.Null(v.Selected);
    }

    [Fact]
    public void HIE_007_a_click_near_any_part_of_a_section_picks_the_section_not_a_unit()
    {
        var (b, v) = InView();
        var at = b.Find(P)!.Position;

        Assert.True(v.Click(new MapPoint(at.X + 2, at.Y - 1.5)));

        Assert.Equal(P, v.Selected);
        Assert.Equal(P, v.PickSection(new MapPoint(at.X, at.Y), Side.Player));
        Assert.Null(v.PickSection(new MapPoint(at.X + 40, at.Y), Side.Player));
    }

    [Fact]
    public void CMD_005_two_placed_waypoints_become_one_move_when_confirmed()
    {
        var (b, v) = InView();
        v.Select(P);
        Assert.True(v.AddWaypoint(new MapPoint(30.4, 20)));
        Assert.True(v.AddWaypoint(new MapPoint(40, 25)));
        Assert.Empty(b.CommandLog); // nothing is a command until confirmed

        Assert.True(v.ConfirmMove(), v.Message);

        Assert.Single(b.CommandLog);
        var move = Assert.IsType<MoveCommand>(b.CommandLog[0].Command);
        Assert.Equal(new[] { new Waypoint(30, 20), new Waypoint(40, 25) }, move.Waypoints);
        Assert.Equal(2, b.Find(P)!.Waypoints.Count);
        Assert.Empty(v.PendingWaypoints);
    }

    [Fact]
    public void CMD_005_confirming_with_no_waypoints_or_no_selection_issues_nothing()
    {
        var (b, v) = InView();
        Assert.False(v.ConfirmMove());
        v.Select(P);
        Assert.False(v.ConfirmMove());
        Assert.False(v.AddWaypoint(new MapPoint(500, 20)));
        Assert.Empty(b.CommandLog);
    }

    [Fact]
    public void CMD_020_a_waypoint_past_the_cap_is_refused_in_the_view()
    {
        var (b, v) = InView();
        v.Select(P);
        v.AddWaypoint(new MapPoint(30, 20));
        v.AddWaypoint(new MapPoint(40, 20));

        Assert.False(v.AddWaypoint(new MapPoint(50, 20)));

        Assert.Equal(2, v.PendingWaypoints.Count);
        StringAssert("P-CMD-01", v.Message);
        Assert.Empty(b.CommandLog);
    }

    [Fact]
    public void CMD_020_already_queued_waypoints_count_towards_the_cap()
    {
        var (b, v) = InView();
        v.Select(P);
        v.AddWaypoint(new MapPoint(30, 20));
        v.ConfirmMove();
        Assert.Equal(1, v.WaypointsLeft);

        Assert.True(v.AddWaypoint(new MapPoint(40, 20)));
        Assert.False(v.AddWaypoint(new MapPoint(50, 20)));
        Assert.True(v.ConfirmMove());

        Assert.Equal(2, b.Find(P)!.Waypoints.Count);
        Assert.Equal(0, v.WaypointsLeft);
    }

    [Fact]
    public void CMD_020_clearing_queued_waypoints_gives_the_allowance_back()
    {
        var (b, v) = InView();
        v.Select(P);
        v.AddWaypoint(new MapPoint(30, 20));
        v.AddWaypoint(new MapPoint(40, 20));
        v.ConfirmMove();
        Assert.Equal(0, v.WaypointsLeft);

        Assert.True(v.ClearWaypoints());

        Assert.Equal(2, v.WaypointsLeft);
        Assert.Empty(b.Find(P)!.Waypoints);
    }

    [Fact]
    public void CMD_006_stop_issues_a_stop_command()
    {
        var (b, v) = InView();
        v.Select(P);
        Assert.True(v.Stop());
        Assert.IsType<StopCommand>(b.CommandLog[0].Command);
        Assert.True(b.Find(P)!.IsHolding);
    }

    [Fact]
    public void CMD_007_clear_waypoints_issues_a_clear_command_and_drops_the_plan()
    {
        var (b, v) = InView();
        v.Select(P);
        v.AddWaypoint(new MapPoint(30, 20));
        v.ConfirmMove();
        v.AddWaypoint(new MapPoint(40, 20));

        Assert.True(v.ClearWaypoints());

        Assert.IsType<ClearWaypointsCommand>(b.CommandLog[1].Command);
        Assert.Empty(v.PendingWaypoints);
    }

    [Fact]
    public void CMD_008_clicking_an_enemy_section_with_a_section_selected_issues_a_target_command()
    {
        var (b, v) = InView();
        v.Select(P);
        var enemyAt = b.Find(E)!.Position;

        Assert.True(v.Click(new MapPoint(enemyAt.X + 1, enemyAt.Y)), v.Message);

        var target = Assert.IsType<TargetCommand>(b.CommandLog[0].Command);
        Assert.Equal(E, target.TargetSection);
        Assert.Equal(E, b.Find(P)!.OrderedTarget);
    }

    [Fact]
    public void CMD_008_clicking_an_enemy_section_with_nothing_selected_issues_nothing()
    {
        var (b, v) = InView();
        var enemyAt = b.Find(E)!.Position;
        // Nothing is selected, so the click falls through to "place waypoint", which is refused.
        Assert.False(v.Click(enemyAt));
        Assert.Empty(b.CommandLog);
    }

    [Fact]
    public void CMD_011_fall_back_issues_a_fall_back_command()
    {
        var (b, v) = InView();
        v.Select(P);
        Assert.True(v.FallBack());
        Assert.IsType<FallBackCommand>(b.CommandLog[0].Command);
        Assert.Single(b.Find(P)!.Waypoints);
    }

    [Fact]
    public void DLV_013_every_order_goes_through_apply_command_and_is_logged_with_its_step()
    {
        var (b, v) = InView();
        b.Step();
        v.Select(P);
        v.AddWaypoint(new MapPoint(30, 20));
        v.ConfirmMove();
        v.Stop();

        Assert.Equal(2, b.CommandLog.Count);
        Assert.All(b.CommandLog, l => Assert.Equal(1, l.Step));
    }

    [Fact]
    public void CMD_020_a_core_rejection_is_reported_and_keeps_the_plan()
    {
        var (b, v) = InView();
        v.Select(P);
        v.AddWaypoint(new MapPoint(30, 20));
        // Something else queues 2 waypoints behind the view's back: the core still enforces the cap.
        b.ApplyCommand(new MoveCommand(P, new[] { new Waypoint(10, 10), new Waypoint(11, 11) }));

        Assert.False(v.ConfirmMove());

        Assert.StartsWith("Refused", v.Message);
        Assert.Single(v.PendingWaypoints);
    }

    [Fact]
    public void CMD_013_the_controls_list_has_no_ability_trigger()
    {
        Assert.DoesNotContain(TacticalView.Controls, c => c.Contains("bilit", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Stop", TacticalView.Controls);
        Assert.Contains("Fall back", TacticalView.Controls);
    }

    private static void StringAssert(string expected, string actual) => Assert.Contains(expected, actual);
}
