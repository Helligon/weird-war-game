using System.Reflection;
using System.Runtime.Versioning;
using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Core.Tests;

public class CoreArchitectureTests
{
    private static readonly Assembly Core = typeof(HexCoord).Assembly;

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
}
