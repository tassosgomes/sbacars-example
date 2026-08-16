namespace SbaCars.Architecture.Tests;

/// <summary>
/// Proves the A9 readiness criterion literally: "referência de projeto indevida entre serviços
/// quebra o build". In SBA (§2.4 of the architecture plan) every dependency between the four
/// domain services — <c>Catalog</c>, <c>Inventory</c>, <c>Interest</c>, <c>Purchase</c> — is an
/// asynchronous integration event, never a compile-time reference; and the two gateways route to
/// services over HTTP (§2.3), never link against one. <c>BuildingBlocks</c> and <c>Contracts</c>
/// are the deliberate exception (§3.3: "bibliotecas compartilhadas são característica, não
/// violação") and stay unrestricted.
/// </summary>
/// <remarks>
/// Checks the *transitive* closure of each project's <c>ProjectReference</c>s, not just the direct
/// ones: a service could in principle reach another service through an intermediate project
/// without a single direct edge crossing the boundary. Today no such indirection exists — every
/// path a service can take leads only to its own projects or to BuildingBlocks/Contracts — but
/// walking the closure is what actually proves that, instead of assuming the graph never grows a
/// second hop.
/// </remarks>
public sealed class ServiceIsolationTests
{
    private static readonly string[] ServiceGroups = ["Catalog", "Inventory", "Interest", "Purchase"];

    // 29 projects live under backend/src as of A9 (BuildingBlocks Domain gains SbaCars.TestKit
    // under tests/, not src/, so it does not raise this number). The margin below that is a
    // sanity floor against an empty/partial scan, not an exact count to keep in lockstep with
    // the solution as it grows.
    private const int MinimumExpectedProjects = 25;

    [Fact]
    public void NoServiceOrGatewayProjectReachesAnotherServicesProject()
    {
        var srcDirectory = RepositoryPaths.BackendSrcDirectory;
        var graph = ProjectReferenceGraph.Build(srcDirectory);

        // Guards against an empty/broken scan reporting a false "no offenders found" (same
        // reasoning as AuthorizationVocabularyTests.MinimumScannedFiles): backend/src holds 29 of
        // the solution's 36 projects as of A9 (the other 7 are test projects, under tests/); a
        // number far below that means the graph was not actually built from backend/src, and the
        // assertion below would pass without checking anything.
        graph.AllProjectPaths.Count.Should().BeGreaterThanOrEqualTo(
            MinimumExpectedProjects,
            $"the project graph built from '{srcDirectory}' must actually reach the solution's projects; " +
            "an empty or partial graph would report no boundary violations without checking anything");

        var offenders = new List<string>();

        foreach (var project in graph.AllProjectPaths)
        {
            var ownGroup = graph.Group(project);
            if (!IsRestricted(ownGroup))
            {
                // BuildingBlocks/Contracts: shared by design, unrestricted (§3.3).
                continue;
            }

            foreach (var reachable in graph.TransitiveReferences(project))
            {
                var targetGroup = graph.Group(reachable);
                if (!CrossesServiceBoundary(ownGroup, targetGroup))
                {
                    continue;
                }

                offenders.Add(
                    $"{RelativeName(srcDirectory, project)} -> {RelativeName(srcDirectory, reachable)}");
            }
        }

        offenders.Should().BeEmpty(
            "no project belonging to one service may reference (directly or transitively) a project " +
            "belonging to another service, and no gateway may reference a service project at all (§2.3, §2.4); " +
            $"offending edges: {string.Join(", ", offenders)}");
    }

    private static bool IsRestricted(string group) => ServiceGroups.Contains(group) || group == "Gateways";

    private static bool CrossesServiceBoundary(string ownGroup, string targetGroup)
    {
        if (ownGroup == targetGroup)
        {
            return false;
        }

        var targetIsService = ServiceGroups.Contains(targetGroup);

        if (ownGroup == "Gateways")
        {
            // A gateway may reference the other gateway project (SbaCars.Gateway.Shared) and
            // BuildingBlocks/Contracts, but never a service — it routes to services over HTTP.
            return targetIsService;
        }

        // ownGroup is one of the four services: it may reference its own projects and
        // BuildingBlocks/Contracts, but neither another service nor a gateway.
        return targetIsService || targetGroup == "Gateways";
    }

    private static string RelativeName(string srcDirectory, string projectPath) =>
        Path.GetRelativePath(srcDirectory, projectPath);
}
