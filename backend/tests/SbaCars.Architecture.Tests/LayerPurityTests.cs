namespace SbaCars.Architecture.Tests;

/// <summary>
/// Enforces §2.2 of the architecture plan for the two innermost layers of Clean Architecture:
/// "<c>Domain</c>: ... Zero dependência de ASP.NET Core, EF Core ou Rebus" and
/// "<c>Application</c>: ... Começa com Service Pattern simples" — neither layer is allowed to know
/// persistence or HTTP framework exists. <c>BuildingBlocks.Domain</c> is the one dependency
/// <c>.Domain</c> projects are allowed (entities/value objects/domain events live there, §3.3);
/// <c>.Application</c> projects add <c>BuildingBlocks.Application</c> and their own <c>.Domain</c>.
/// </summary>
/// <remarks>
/// Checks the *transitive* closure of <c>ProjectReference</c>s via <see cref="ProjectReferenceGraph"/>,
/// not a text scan for package names inside the <c>.Domain</c>/<c>.Application</c> project's own
/// <c>.csproj</c>. That distinction matters: today no service's <c>.Domain</c> or <c>.Application</c>
/// carries a direct <c>PackageReference</c> to EF Core or ASP.NET Core — the real risk is a single
/// stray <c>ProjectReference</c> to <c>BuildingBlocks.Persistence</c> or <c>BuildingBlocks.Web</c>
/// (both of which *do* carry those frameworks), which a same-file text scan would never see. This
/// is the same class of regression <c>ReverseProxyContainmentTests</c>' remarks describe for A7 —
/// a shared package leaking into every consumer of a project that should never have carried it.
/// </remarks>
public sealed class LayerPurityTests
{
    private const int MinimumCheckedProjects = 4;

    [Theory]
    [InlineData(".Domain")]
    [InlineData(".Application")]
    public void ProjectsOfThisLayer_NeverReachEfCoreOrAspNetCore(string layerSuffix)
    {
        var srcDirectory = RepositoryPaths.BackendSrcDirectory;
        var graph = ProjectReferenceGraph.Build(srcDirectory);

        var layerProjects = graph.AllProjectPaths
            .Where(project => Path.GetFileNameWithoutExtension(project).EndsWith(layerSuffix, StringComparison.Ordinal))
            .ToArray();

        // Same reasoning as ServiceIsolationTests' minimum-project guard: the four services each
        // contribute exactly one project per layer, so finding fewer than that means the scan
        // missed projects rather than that the codebase has none to check.
        layerProjects.Length.Should().BeGreaterThanOrEqualTo(
            MinimumCheckedProjects,
            $"expected at least one '{layerSuffix}' project per service; finding fewer means the scan " +
            $"of '{srcDirectory}' did not actually reach the solution's projects");

        var offenders = new List<string>();

        foreach (var project in layerProjects)
        {
            foreach (var reachable in graph.TransitiveReferences(project).Prepend(project))
            {
                if (!graph.DeclaresEfCore(reachable) && !graph.DeclaresAspNetCore(reachable))
                {
                    continue;
                }

                var framework = graph.DeclaresEfCore(reachable) ? "EF Core" : "ASP.NET Core";
                offenders.Add(
                    $"{Path.GetFileName(project)} reaches {framework} through {Path.GetFileName(reachable)}");
            }
        }

        offenders.Should().BeEmpty(
            $"'{layerSuffix}' projects must have zero dependency on EF Core or ASP.NET Core, direct or " +
            $"transitive (§2.2); offending paths: {string.Join(", ", offenders)}");
    }
}
