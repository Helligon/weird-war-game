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

    private static readonly string[] CompilerNamespaces = { "Microsoft.CodeAnalysis", "System.Runtime.CompilerServices" };

    /// <summary>
    /// True for types the compiler adds to an assembly (embedded nullable attributes, the
    /// private-implementation-details holder for array initialisers). Narrow on purpose: the outermost type
    /// must carry CompilerGenerated or Microsoft.CodeAnalysis.EmbeddedAttribute (matched by full name) AND live in
    /// one of the compiler namespaces or be named &lt;PrivateImplementationDetails&gt;. Nested types follow
    /// their outermost type. Our own types in WeirdWar.Core.* are never skipped.
    /// </summary>
    internal static bool IsCompilerSupportType(Type type)
    {
        var top = type;
        while (top.DeclaringType != null) top = top.DeclaringType;

        var marked = top.GetCustomAttributes(false).Any(a =>
            a.GetType().FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"
            || a.GetType().FullName == "Microsoft.CodeAnalysis.EmbeddedAttribute");
        if (!marked) return false;

        return top.Name == "<PrivateImplementationDetails>"
            || (top.Namespace != null && CompilerNamespaces.Contains(top.Namespace));
    }

    [Fact]
    public void TEC_013_core_types_all_live_in_the_core_namespace()
    {
        var checkedTypes = Core.GetTypes().Where(t => !IsCompilerSupportType(t)).ToList();

        var outside = checkedTypes
            .Where(t => !t.Name.StartsWith("<") && t.Namespace != "WeirdWar.Core"
                        && !(t.Namespace ?? "").StartsWith("WeirdWar.Core."))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(outside);
        Assert.Contains(checkedTypes, t => t == typeof(CoreAssembly)); // our own types are still checked
    }

    [Fact]
    public void TEC_013_guard_skips_real_compiler_types_but_never_our_own()
    {
        var skipped = Core.GetTypes().Where(IsCompilerSupportType).ToList();

        Assert.Contains(skipped, t => t.FullName == "Microsoft.CodeAnalysis.EmbeddedAttribute");
        Assert.All(skipped, t => Assert.DoesNotContain("WeirdWar.Core", t.Namespace ?? ""));
        Assert.False(IsCompilerSupportType(typeof(CoreAssembly)));
    }

    [Fact]
    public void TEC_013_guard_is_not_fooled_by_look_alike_attributes_or_namespaces()
    {
        // Home-made EmbeddedAttribute in another namespace, on a type in a compiler namespace.
        Assert.False(IsCompilerSupportType(typeof(Microsoft.CodeAnalysis.FakeMarkedType)));
        // Real CompilerGenerated, but the type is not in a compiler namespace.
        Assert.False(IsCompilerSupportType(typeof(GeneratedLookingTypeInOurNamespace)));
        // Right namespace, no marker at all.
        Assert.False(IsCompilerSupportType(typeof(Microsoft.CodeAnalysis.UnmarkedType)));
        // A nested type inside a look-alike is not skipped either.
        Assert.False(IsCompilerSupportType(typeof(Microsoft.CodeAnalysis.FakeMarkedType.Nested)));
    }

    [System.Runtime.CompilerServices.CompilerGenerated]
    private sealed class GeneratedLookingTypeInOurNamespace { }

    [Fact]
    public void TEC_013_guard_finds_the_project_files_it_scans()
    {
        var files = ProjectAndSourceFiles().Select(Path.GetFileName).ToList();

        Assert.Contains("WeirdWar.Core.csproj", files);
        Assert.Contains("WeirdWar.Core.Tests.csproj", files);
    }

    [Fact]
    public void DLV_012_the_fog_of_war_code_uses_no_unity_wall_clock_random_or_hash_order_source()
    {
        var files = new[] { "FogOfWar.cs", "FactionView.cs", "LocationSight.cs", "Forces.cs" };
        var banned = new[] { "UnityEngine", "DateTime", "Stopwatch", "Environment.Tick", "System.Random", "new Random(", "Guid.NewGuid", "GetHashCode", "RuntimeHelpers", "Parallel", "ConcurrentDictionary" };
        var simulation = Path.Combine(SrcDirectory(), "WeirdWar.Core", "Simulation");
        foreach (var name in files)
        {
            var text = File.ReadAllText(Path.Combine(simulation, name));
            foreach (var word in banned) Assert.False(text.Contains(word, StringComparison.Ordinal), name + " uses " + word);
        }
        // The one hash set in the fog code is sorted before it leaves the method, never enumerated as it is.
        var fog = File.ReadAllText(Path.Combine(simulation, "FogOfWar.cs"));
        Assert.Contains("result.Sort(", fog);
        Assert.DoesNotContain("foreach (var h in seen", fog);
        Assert.DoesNotContain("foreach (var hex in seen", fog);
    }
}
