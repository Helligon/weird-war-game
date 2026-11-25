using System.Reflection;
using System.Runtime.Versioning;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Battle.Core.Tests;

public class CoreArchitectureTests
{
    private static readonly Assembly Core = typeof(IBattleCommand).Assembly;

    [Fact]
    public void DLV_012_core_has_no_unity_references()
    {
        var unityReferences = Core.GetReferencedAssemblies()
            .Where(a => a.Name != null && a.Name.StartsWith("Unity", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Name)
            .ToList();

        Assert.Empty(unityReferences);
    }

    [Fact]
    public void TEC_012_core_targets_netstandard_2_1()
    {
        var framework = Core.GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.NotNull(framework);
        Assert.Equal(".NETStandard,Version=v2.1", framework!.FrameworkName);
    }

    [Fact]
    public void DLV_013_command_interface_is_a_plain_data_marker_in_the_core()
    {
        var type = typeof(IBattleCommand);

        Assert.Same(Core, type.Assembly);
        Assert.True(type.IsInterface);
        // No behaviour: a command only carries data, and state changes happen elsewhere.
        Assert.Empty(type.GetMethods());
        Assert.Empty(type.GetProperties());
        Assert.Empty(type.GetEvents());
    }
}
