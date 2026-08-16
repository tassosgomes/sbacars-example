using System.Text.RegularExpressions;

namespace SbaCars.Architecture.Tests;

/// <summary>
/// Enforces §4.2 of the architecture plan: "Sem <c>DbContext</c> fora da Infrastructure — verificado
/// por teste de arquitetura." Only a project whose name ends in <c>.Infrastructure</c> may declare a
/// concrete (non-abstract) class deriving from <c>DbContext</c>.
/// </summary>
/// <remarks>
/// <b>Why a source scan instead of loading compiled assemblies (NetArchTest/ArchUnitNET).</b> This
/// rule needs to tell abstract from concrete, which a plain text scan for the package name (the
/// mechanic <c>ReverseProxyContainmentTests</c>/<c>ObservabilityContainmentTests</c> use) cannot do —
/// <c>SbaCarsDbContext</c> itself lives in <c>BuildingBlocks.Persistence</c>, deliberately (§3.3:
/// "Concrete implementations live exclusively in each service's Infrastructure project — never in
/// Api, Application or Domain"), and it derives from <c>DbContext</c> too. NetArchTest.Rules 1.3.2
/// (verified against NuGet before adopting anything) does distinguish abstract types via
/// <c>Mono.Cecil</c>-based reflection over compiled assemblies, but pulling it in would mean this
/// project either references every <c>.Infrastructure</c> project it polices (the same coupling
/// <see cref="ProjectReferenceGraph"/> exists to avoid) or points reflection at build output paths,
/// which is a config-dependent (Debug/Release, per-TFM) path this project does not otherwise need to
/// know. A class-declaration regex over <c>backend/src/**/*.cs</c> — matching the line, checking
/// "abstract" is not among its modifiers, and checking the base-type list mentions <c>DbContext</c>
/// — reaches the same answer without either cost, in the same coarse-but-safe spirit
/// <see cref="AuthorizationVocabularyTests"/>'s remarks describe. It only recognizes single-line
/// declarations (this codebase's own consistent style, e.g. <c>public sealed class CatalogDbContext
/// : SbaCarsDbContext</c>), which is the deliberate boundary of "coarse": a class declared across
/// multiple lines would be missed, which is why <see cref="AtLeastTheFourKnownServiceContextsAreFound"/>
/// exists as a positive control — proof the scan actually finds real <c>DbContext</c> subclasses,
/// not just that it finds zero of everything.
/// </remarks>
public sealed class DbContextContainmentTests
{
    private static readonly Regex ClassDeclarationWithBaseList = new(
        @"^[ \t]*(?<modifiers>(?:\w+\s+)+)class\s+(?<name>\w+)(?:<[^>]*>)?\s*(?:\([^)]*\))?\s*:\s*(?<bases>[^{]+)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// A generic type parameter's <c>where TContext : SbaCarsDbContext</c> constraint reads as
    /// text exactly like a base-type mention — <c>EfUnitOfWork&lt;TContext&gt; : IUnitOfWork
    /// where TContext : SbaCarsDbContext</c> would otherwise falsely look like a class extending
    /// <c>SbaCarsDbContext</c>. Stripping everything from the <c>where</c> keyword onward before
    /// checking for "DbContext" removes the false positive without needing a real C# parser.
    /// </summary>
    private static readonly Regex GenericConstraintClause = new(
        @"\bwhere\b.*", RegexOptions.Singleline | RegexOptions.Compiled);

    [Fact]
    public void OnlyInfrastructureProjects_DeclareAConcreteDbContext()
    {
        var offenders = FindConcreteDbContextDeclarations()
            .Where(declaration => !IsUnderInfrastructureProject(declaration.FilePath))
            .Select(declaration => $"{declaration.RelativePath}: {declaration.ClassName}")
            .ToArray();

        offenders.Should().BeEmpty(
            "a concrete DbContext must live only in a *.Infrastructure project (§4.2) — SbaCarsDbContext " +
            "itself stays abstract in BuildingBlocks.Persistence for every service to derive from; " +
            $"found outside Infrastructure: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// Positive control (see class remarks): proves the regex actually recognizes real
    /// <c>DbContext</c> subclasses instead of the assertion above passing because nothing matched
    /// at all. The four services' own contexts — <c>CatalogDbContext</c>, <c>InventoryDbContext</c>,
    /// <c>InterestDbContext</c>, <c>PurchaseDbContext</c> — are exactly what must be found.
    /// </summary>
    [Fact]
    public void AtLeastTheFourKnownServiceContextsAreFound()
    {
        var found = FindConcreteDbContextDeclarations()
            .Select(declaration => declaration.ClassName)
            .ToHashSet(StringComparer.Ordinal);

        found.Should().Contain(
            ["CatalogDbContext", "InventoryDbContext", "InterestDbContext", "PurchaseDbContext"],
            "the scan must actually detect the four services' real DbContext subclasses; finding fewer " +
            "means the class-declaration regex silently stopped matching, not that they don't exist");
    }

    private static IEnumerable<(string FilePath, string RelativePath, string ClassName)> FindConcreteDbContextDeclarations()
    {
        var srcDirectory = RepositoryPaths.BackendSrcDirectory;

        foreach (var file in Directory.EnumerateFiles(srcDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);

            foreach (Match match in ClassDeclarationWithBaseList.Matches(content))
            {
                if (match.Groups["modifiers"].Value.Contains("abstract", StringComparison.Ordinal))
                {
                    continue;
                }

                var baseList = GenericConstraintClause.Replace(match.Groups["bases"].Value, string.Empty);
                if (!baseList.Contains("DbContext", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return (file, Path.GetRelativePath(srcDirectory, file), match.Groups["name"].Value);
            }
        }
    }

    private static bool IsUnderInfrastructureProject(string filePath) =>
        filePath.Split(Path.DirectorySeparatorChar)
            .Any(segment => segment.EndsWith(".Infrastructure", StringComparison.Ordinal));
}
