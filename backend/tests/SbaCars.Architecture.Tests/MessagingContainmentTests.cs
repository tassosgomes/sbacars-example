namespace SbaCars.Architecture.Tests;

/// <summary>
/// Keeps the messaging transport out of the layers that must not know about it (§2.2, §3.3, §6.3.1
/// of the architecture plan): <c>Domain</c>/<c>Application</c> only ever see
/// <c>IIntegrationEventPublisher</c> and <c>IOutboxTransaction</c> (§6, D5/B2 of the task spec),
/// never <c>Rebus</c> or <c>RabbitMQ.Client</c> directly — the concrete bus adapter lives in
/// <c>BuildingBlocks.Messaging</c>, and the outbox enlistment glue lives in
/// <c>BuildingBlocks.Persistence</c> (<see cref="EfUnitOfWork{TContext}"/>), the same split
/// <see cref="DbContextContainmentTests"/> already enforces for <c>DbContext</c>. It also keeps the
/// broker off the edge: §6.3.1 sizes the whole connection budget against exactly four bus connections
/// (one per domain service) plus their readiness probes — a gateway that could reach
/// <c>BuildingBlocks.Messaging</c> would break that count, and §2.3 already keeps gateways HTTP-only
/// towards services in the first place.
/// </summary>
public sealed class MessagingContainmentTests
{
    private static readonly string[] MessagingTransportPackages = ["Rebus", "RabbitMQ.Client"];

    [Fact]
    public void NoDomainOrApplicationProjectReferencesRebusOrRabbitMqClient()
    {
        var offenders = Directory
            .EnumerateFiles(RepositoryPaths.BackendSrcDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(IsDomainOrApplicationProject)
            .Where(ReferencesMessagingTransportPackageDirectly)
            .Select(Path.GetFileName)
            .ToArray();

        offenders.Should().BeEmpty(
            "Domain and Application must depend only on IIntegrationEventPublisher/IOutboxTransaction (§2.2, §6, D5/B2) — " +
            "Rebus/RabbitMQ.Client belong in BuildingBlocks.Messaging and BuildingBlocks.Persistence, not here; found in: " +
            string.Join(", ", offenders));
    }

    /// <summary>
    /// The transitive counterpart of the direct-reference check above: a <c>.Domain</c>/
    /// <c>.Application</c> project could in principle reach <c>BuildingBlocks.Messaging</c> — and
    /// therefore Rebus/RabbitMQ.Client — through an intermediate <c>ProjectReference</c> without a
    /// single direct edge crossing the boundary. Today no such indirection exists (every path a
    /// service's Domain/Application can take leads only to BuildingBlocks.Domain/Application
    /// themselves), but walking the closure is what actually proves that, exactly as
    /// <see cref="ServiceIsolationTests"/> does for the service boundary.
    /// </summary>
    [Fact]
    public void NoDomainOrApplicationProjectTransitivelyReachesBuildingBlocksMessaging()
    {
        var srcDirectory = RepositoryPaths.BackendSrcDirectory;
        var graph = ProjectReferenceGraph.Build(srcDirectory);

        var offenders = graph.AllProjectPaths
            .Where(IsDomainOrApplicationProject)
            .Where(project => graph.TransitiveReferences(project).Any(reachable =>
                Path.GetFileNameWithoutExtension(reachable) == "SbaCars.BuildingBlocks.Messaging"))
            .Select(project => Path.GetRelativePath(srcDirectory, project))
            .ToArray();

        offenders.Should().BeEmpty(
            "no Domain or Application project may reach BuildingBlocks.Messaging even transitively (§2.2, §6, D5); " +
            $"offending projects: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void NoGatewayProjectReachesBuildingBlocksMessaging()
    {
        var srcDirectory = RepositoryPaths.BackendSrcDirectory;
        var graph = ProjectReferenceGraph.Build(srcDirectory);

        var offenders = graph.AllProjectPaths
            .Where(project => graph.Group(project) == "Gateways")
            .Where(project => graph.TransitiveReferences(project).Any(reachable =>
                Path.GetFileNameWithoutExtension(reachable) == "SbaCars.BuildingBlocks.Messaging"))
            .Select(project => Path.GetRelativePath(srcDirectory, project))
            .ToArray();

        offenders.Should().BeEmpty(
            "only the four domain services talk to the broker — gateways route over HTTP and never " +
            $"reach BuildingBlocks.Messaging (§2.3, §6.3.1); offending projects: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// <c>SbaCars.Contracts</c> must stay dependency-free by construction (D4): the
    /// <c>[IntegrationEvent]</c> attribute and <c>IIntegrationEvent</c> marker live there specifically
    /// so every one of the four services can reference it without also pulling in Rebus — a single
    /// <c>ProjectReference</c> or <c>PackageReference</c> here would defeat that.
    /// </summary>
    [Fact]
    public void ContractsProjectHasNoProjectOrPackageReferenceAtAll()
    {
        var srcDirectory = RepositoryPaths.BackendSrcDirectory;
        var graph = ProjectReferenceGraph.Build(srcDirectory);

        var contractsProject = graph.AllProjectPaths
            .Should().ContainSingle(project => Path.GetFileNameWithoutExtension(project) == "SbaCars.Contracts")
            .Subject;

        graph.DirectReferences(contractsProject).Should().BeEmpty(
            "SbaCars.Contracts is the wire vocabulary every service references (D4) — a ProjectReference " +
            "here would become a transitive dependency for all of them");

        var contractsContent = File.ReadAllText(contractsProject);
        contractsContent.Should().NotContain(
            "<PackageReference", "SbaCars.Contracts must have zero PackageReference by construction (D4)");
    }

    /// <summary>
    /// Positive control, in the same spirit as
    /// <see cref="DbContextContainmentTests.AtLeastTheFourKnownServiceContextsAreFound"/>: proves the
    /// scan actually finds a real reference to the transport packages, so the assertions above pass
    /// because the rule holds, not because the scan never matches anything.
    /// The two rules this guards were each also proven failing first, on purpose, against a
    /// deliberately introduced violation before it was reverted:
    /// <see cref="NoDomainOrApplicationProjectReferencesRebusOrRabbitMqClient"/> against a
    /// <c>Rebus</c> <c>PackageReference</c> added to <c>SbaCars.Inventory.Domain</c>, and
    /// <see cref="NoGatewayProjectReachesBuildingBlocksMessaging"/> against a
    /// <c>BuildingBlocks.Messaging</c> <c>ProjectReference</c> added to <c>SbaCars.Gateway.Public</c>
    /// — the latter exercising the transitive-closure path rather than this text scan.
    /// </summary>
    [Fact]
    public void BuildingBlocksMessagingItselfReferencesRebusAndRabbitMqClient()
    {
        var messagingProject = Directory
            .EnumerateFiles(RepositoryPaths.BackendSrcDirectory, "SbaCars.BuildingBlocks.Messaging.csproj", SearchOption.AllDirectories)
            .Should().ContainSingle()
            .Subject;

        var content = File.ReadAllText(messagingProject);

        MessagingTransportPackages.Should().AllSatisfy(package =>
            content.Should().Contain(package,
                $"the scan must actually detect a real '{package}' reference; finding none means the " +
                "text-scan mechanic silently stopped matching, not that BuildingBlocks.Messaging stopped using it"));
    }

    private static bool IsDomainOrApplicationProject(string projectPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(projectPath);
        return fileName.EndsWith(".Domain", StringComparison.Ordinal) ||
               fileName.EndsWith(".Application", StringComparison.Ordinal);
    }

    private static bool ReferencesMessagingTransportPackageDirectly(string projectPath)
    {
        var content = File.ReadAllText(projectPath);
        return MessagingTransportPackages.Any(package =>
            content.Contains(package, StringComparison.Ordinal));
    }
}
