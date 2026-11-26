using System.Reflection;
using System.Runtime.Versioning;
using System.Xml.Linq;
using WeirdWar.Core;

namespace WeirdWar.Core.Tests;

public class CoreArchitectureTests
{
    private static readonly Assembly Core = typeof(CoreAssembly).Assembly;

    private static string SrcDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "WeirdWar.Core", "WeirdWar.Core.csproj")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static IEnumerable<string> ProjectAndSourceFiles()
    {
        var sep = Path.DirectorySeparatorChar;
        return Directory.EnumerateFiles(SrcDirectory(), "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{sep}bin{sep}") && !f.Contains($"{sep}obj{sep}"))
            .Where(f => f.EndsWith(".csproj") || f.EndsWith(".cs") || f.EndsWith(".props")
                     || f.EndsWith(".targets") || f.EndsWith(".sln") || f.EndsWith(".slnx")
                     || f.EndsWith(".json"));
    }

    [Fact]
    public void TEC_016_core_targets_netstandard_2_1()
    {
        var framework = Core.GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.NotNull(framework);
        Assert.Equal(".NETStandard,Version=v2.1", framework!.FrameworkName);
    }

    [Fact]
    public void DLV_012_core_has_no_unity_assembly_references()
    {
        var unity = Core.GetReferencedAssemblies()
            .Where(a => a.Name != null && a.Name.StartsWith("Unity", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Name)
            .ToList();

        Assert.Empty(unity);
    }

    [Fact]
    public void TEC_012_core_project_file_has_no_unity_reference_or_package()
    {
        var msbuildFiles = ProjectAndSourceFiles()
            .Where(f => f.EndsWith(".csproj") || f.EndsWith(".props") || f.EndsWith(".targets"));

        var references = msbuildFiles
            .SelectMany(f => XDocument.Load(f).Descendants())
            .Where(e => e.Name.LocalName is "Reference" or "PackageReference" or "ProjectReference" or "HintPath")
            .Select(e => (string?)e.Attribute("Include") ?? e.Value)
            .ToList();

        Assert.DoesNotContain(references, r => r.Contains("Unity", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TEC_013_nothing_under_src_references_prototypes()
    {
        var offenders = ProjectAndSourceFiles()
            .Where(f => Path.GetFileName(f) != "CoreArchitectureTests.cs") // this guard names the folder on purpose
            .Where(f => File.ReadAllText(f).Contains("prototypes", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void TEC_013_core_assembly_contains_no_spike_types()
    {
        var spikeTypes = Core.GetTypes()
            .Where(t => (t.Namespace ?? "").StartsWith("WeirdWar.Spike", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(spikeTypes);
    }

    [Fact]
    public void TEC_013_core_assembly_references_no_spike_assembly()
    {
        var spike = Core.GetReferencedAssemblies()
            .Where(a => a.Name != null && a.Name.StartsWith("WeirdWar.Spike", StringComparison.Ordinal))
            .Select(a => a.Name)
            .ToList();

        Assert.Empty(spike);
    }

    [Fact]
    public void TEC_013_core_types_all_live_in_the_core_namespace()
    {
        var outside = Core.GetTypes()
            // The compiler embeds its own nullable-annotation attribute types (marked EmbeddedAttribute)
            // into netstandard2.1 assemblies. They are not our types.
            .Where(t => !t.GetCustomAttributes(false).Any(a => a.GetType().Name == "EmbeddedAttribute"))
            .Where(t => !t.Name.StartsWith("<") && t.Namespace != "WeirdWar.Core"
                        && !(t.Namespace ?? "").StartsWith("WeirdWar.Core."))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(outside);
    }

    [Fact]
    public void TEC_013_guard_finds_the_project_files_it_scans()
    {
        var files = ProjectAndSourceFiles().Select(Path.GetFileName).ToList();

        Assert.Contains("WeirdWar.Core.csproj", files);
        Assert.Contains("WeirdWar.Core.Tests.csproj", files);
    }
}
