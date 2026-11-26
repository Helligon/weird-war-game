using System.Reflection;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

public class CommandTests
{
    private static readonly SectionId Mine = new SectionId(1);
    private static readonly SectionId Friend = new SectionId(2);
    private static readonly SectionId Foe = new SectionId(3);

    private sealed class FakeView : IBattleView
    {
        private readonly Dictionary<int, SectionInfo> _sections = new();

        public FakeView(int queuedForMine)
        {
            _sections[1] = new SectionInfo(Side.Player, queuedForMine);
            _sections[2] = new SectionInfo(Side.Player, 0);
            _sections[3] = new SectionInfo(Side.Enemy, 0);
        }

        public bool TryGetSection(SectionId id, out SectionInfo info) => _sections.TryGetValue(id.Value, out info);
    }

    private static string DataFile() => Path.Combine(AppContext.BaseDirectory, "data", "parameters.json");

    private static BattleParameters Params() => BattleParameters.Load(File.ReadAllText(DataFile()));

    private static MoveCommand MoveOf(int n) =>
        new MoveCommand(Mine, Enumerable.Range(0, n).Select(i => new Waypoint(i, i)));

    private static ValidationResult Check(IBattleCommand c, int queued = 0, BattleParameters? p = null) =>
        CommandValidator.Validate(c, new FakeView(queued), p ?? Params());

    // True for arrays, lists, sets and any other collection of SectionId.
    private static bool TakesSectionIds(Type t) =>
        (t.IsArray && t.GetElementType() == typeof(SectionId))
        || (t.IsGenericType && t.GetGenericArguments().Contains(typeof(SectionId)))
        || t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericArguments().Contains(typeof(SectionId)));

    private static IBattleCommand[] OneOfEach() => new IBattleCommand[]
    {
        MoveOf(2), new StopCommand(Mine), new ClearWaypointsCommand(Mine), new TargetCommand(Mine, Foe), new FallBackCommand(Mine),
    };

    [Fact]
    public void DLV_013_each_command_is_plain_data_with_no_unity_dependency()
    {
        foreach (var command in OneOfEach())
        {
            var type = command.GetType();
            Assert.Same(typeof(IBattleCommand).Assembly, type.Assembly);
            Assert.IsAssignableFrom<IBattleCommand>(command);
            // Data only: no public methods of its own beyond object's and property getters.
            var own = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.Name != "Equals" && m.Name != "GetHashCode");
            Assert.Empty(own);
        }
        Assert.DoesNotContain(typeof(IBattleCommand).Assembly.GetReferencedAssemblies(),
            a => a.Name!.StartsWith("Unity", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void CMD_005_move_with_one_or_two_waypoints_on_empty_queue_is_accepted(int count)
    {
        Assert.True(Check(MoveOf(count)).IsAccepted);
    }

    [Fact]
    public void CMD_005_move_with_no_waypoints_is_rejected()
    {
        Assert.False(Check(MoveOf(0)).IsAccepted);
    }

    [Fact]
    public void CMD_020_move_appends_up_to_the_cap_and_rejects_beyond_it()
    {
        Assert.True(Check(MoveOf(1), queued: 1).IsAccepted);

        var over = Check(MoveOf(2), queued: 1);
        Assert.False(over.IsAccepted);
        Assert.Contains("P-CMD-01", over.Reason);

        Assert.False(Check(MoveOf(3), queued: 0).IsAccepted);
    }

    [Fact]
    public void CMD_020_P_CMD_01_cap_is_read_from_the_data_file_not_hard_coded()
    {
        var path = Path.Combine(Path.GetTempPath(), "ww-params-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, "{\"P-CMD-01\": 3, \"P-CMD-03\": 0.5, \"P-DAI-01\": 25, \"P-DAI-02\": 6, \"P-DAI-03\": 2, \"P-DAI-04\": 0.5, \"P-CMD-04\": 3}");
            var raised = BattleParameters.Load(File.ReadAllText(path));
            Assert.Equal(3, raised.MaxWaypoints);
            Assert.True(Check(MoveOf(3), 0, raised).IsAccepted);
            Assert.False(Check(MoveOf(4), 0, raised).IsAccepted);

            File.WriteAllText(path, "{\"P-CMD-01\": 1, \"P-CMD-03\": 0.5, \"P-DAI-01\": 25, \"P-DAI-02\": 6, \"P-DAI-03\": 2, \"P-DAI-04\": 0.5, \"P-CMD-04\": 3}");
            var lowered = BattleParameters.Load(File.ReadAllText(path));
            Assert.True(Check(MoveOf(1), 0, lowered).IsAccepted);
            Assert.False(Check(MoveOf(2), 0, lowered).IsAccepted);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DLV_014_a_missing_P_CMD_01_is_an_error_not_a_default()
    {
        Assert.Throws<CommandFormatException>(() => BattleParameters.Load("{}"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CMD_006_stop_is_accepted_with_or_without_queued_waypoints(int queued)
    {
        Assert.True(Check(new StopCommand(Mine), queued).IsAccepted);
    }

    [Fact]
    public void CMD_007_clear_waypoints_is_accepted()
    {
        Assert.True(Check(new ClearWaypointsCommand(Mine)).IsAccepted);
        Assert.True(Check(new ClearWaypointsCommand(Mine), queued: 2).IsAccepted);
    }

    [Fact]
    public void CMD_008_target_accepts_an_enemy_and_rejects_a_friendly_section()
    {
        Assert.True(Check(new TargetCommand(Mine, Foe)).IsAccepted);
        Assert.False(Check(new TargetCommand(Mine, Friend)).IsAccepted);
    }

    [Fact]
    public void CMD_016_command_for_unknown_section_is_rejected()
    {
        var unknown = new SectionId(99);
        var commands = new IBattleCommand[]
        {
            new MoveCommand(unknown, new[] { new Waypoint(0, 0) }), new StopCommand(unknown), new ClearWaypointsCommand(unknown),
            new TargetCommand(unknown, Foe), new FallBackCommand(unknown),
        };
        foreach (var command in commands)
        {
            Assert.False(Check(command).IsAccepted);
        }
    }

    [Fact]
    public void CMD_008_target_naming_unknown_section_is_rejected()
    {
        Assert.False(Check(new TargetCommand(Mine, new SectionId(99))).IsAccepted);
    }

    [Fact]
    public void CMD_011_fall_back_is_accepted()
    {
        Assert.True(Check(new FallBackCommand(Mine)).IsAccepted);
        Assert.True(Check(new FallBackCommand(Mine), queued: 2).IsAccepted);
    }

    [Fact]
    public void CMD_016_every_command_carries_exactly_one_section_id_and_no_list_of_sections()
    {
        foreach (var command in OneOfEach())
        {
            var props = command.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Assert.Single(props, p => p.Name == "Section" && p.PropertyType == typeof(SectionId));
            Assert.DoesNotContain(props, p => p.PropertyType != typeof(SectionId) && TakesSectionIds(p.PropertyType));
            foreach (var ctor in command.GetType().GetConstructors())
            {
                Assert.DoesNotContain(ctor.GetParameters(), p => p.ParameterType != typeof(SectionId)
                    && TakesSectionIds(p.ParameterType));
            }
        }
    }

    [Fact]
    public void HIE_007_commands_refer_to_sections_and_never_to_unit_ids()
    {
        foreach (var command in OneOfEach())
        {
            foreach (var p in command.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.DoesNotContain("unit", p.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("unit", p.PropertyType.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
        Assert.DoesNotContain(typeof(IBattleCommand).Assembly.GetTypes(),
            t => t.Name.Contains("UnitId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TEC_008_every_command_round_trips_through_json()
    {
        foreach (var command in OneOfEach().Append(MoveOf(1)))
        {
            var json = CommandSerialiser.Serialise(command);
            var back = CommandSerialiser.Deserialise(json);
            Assert.Equal(command, back);
            Assert.Equal(json, CommandSerialiser.Serialise(back));
        }
    }

    [Fact]
    public void TEC_008_unreadable_command_json_is_a_clear_error()
    {
        Assert.Throws<CommandFormatException>(() => CommandSerialiser.Deserialise("{\"type\":\"dance\",\"section\":1}"));
        Assert.Throws<CommandFormatException>(() => CommandSerialiser.Deserialise("[1]"));
        Assert.Throws<CommandFormatException>(() => CommandSerialiser.Deserialise("{\"type\":"));
    }
}
