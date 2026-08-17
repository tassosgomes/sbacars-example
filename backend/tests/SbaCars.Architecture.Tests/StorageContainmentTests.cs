namespace SbaCars.Architecture.Tests;

/// <summary>
/// Keeps the S3 transport out of the layers that must not know about it (§2.2, §3.3, §7):
/// <c>Domain</c>/<c>Application</c> only see <see cref="SbaCars.BuildingBlocks.Application.IObjectStorage"/>,
/// never <c>AWSSDK.S3</c> directly — the concrete adapter lives in
/// <c>BuildingBlocks.Storage</c>. Gateways stay HTTP-only and must not reach Storage either.
/// </summary>
public sealed class StorageContainmentTests
{
    private const string StorageTransportPackage = "AWSSDK.S3";

    [Fact]
    public void NoDomainOrApplicationProjectReferencesAwsSdkS3()
    {
        var offenders = Directory
            .EnumerateFiles(RepositoryPaths.BackendSrcDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(IsDomainOrApplicationProject)
            .Where(ReferencesStorageTransportPackageDirectly)
            .Select(Path.GetFileName)
            .ToArray();

        offenders.Should().BeEmpty(
            "Domain and Application must depend only on IObjectStorage (§2.2, §7) — " +
            "AWSSDK.S3 belongs in BuildingBlocks.Storage, not here; found in: " +
            string.Join(", ", offenders));
    }

    [Fact]
    public void NoDomainOrApplicationProjectTransitivelyReachesBuildingBlocksStorage()
    {
        var srcDirectory = RepositoryPaths.BackendSrcDirectory;
        var graph = ProjectReferenceGraph.Build(srcDirectory);

        var offenders = graph.AllProjectPaths
            .Where(IsDomainOrApplicationProject)
            .Where(project => graph.TransitiveReferences(project).Any(reachable =>
                Path.GetFileNameWithoutExtension(reachable) == "SbaCars.BuildingBlocks.Storage"))
            .Select(project => Path.GetRelativePath(srcDirectory, project))
            .ToArray();

        offenders.Should().BeEmpty(
            "no Domain or Application project may reach BuildingBlocks.Storage even transitively (§2.2, §7); " +
            $"offending projects: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void NoGatewayProjectReachesBuildingBlocksStorage()
    {
        var srcDirectory = RepositoryPaths.BackendSrcDirectory;
        var graph = ProjectReferenceGraph.Build(srcDirectory);

        var offenders = graph.AllProjectPaths
            .Where(project => graph.Group(project) == "Gateways")
            .Where(project => graph.TransitiveReferences(project).Any(reachable =>
                Path.GetFileNameWithoutExtension(reachable) == "SbaCars.BuildingBlocks.Storage"))
            .Select(project => Path.GetRelativePath(srcDirectory, project))
            .ToArray();

        offenders.Should().BeEmpty(
            "gateways route over HTTP and never reach BuildingBlocks.Storage (§2.3, §7); " +
            $"offending projects: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void BuildingBlocksStorageItselfReferencesAwsSdkS3()
    {
        var storageProject = Directory
            .EnumerateFiles(RepositoryPaths.BackendSrcDirectory, "SbaCars.BuildingBlocks.Storage.csproj", SearchOption.AllDirectories)
            .Should().ContainSingle()
            .Subject;

        var content = File.ReadAllText(storageProject);

        content.Should().Contain(StorageTransportPackage,
            "the scan must actually detect a real AWSSDK.S3 reference; finding none means the " +
            "text-scan mechanic silently stopped matching, not that BuildingBlocks.Storage stopped using it");
    }

    private static bool IsDomainOrApplicationProject(string projectPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(projectPath);
        return fileName.EndsWith(".Domain", StringComparison.Ordinal) ||
               fileName.EndsWith(".Application", StringComparison.Ordinal);
    }

    private static bool ReferencesStorageTransportPackageDirectly(string projectPath)
    {
        var content = File.ReadAllText(projectPath);
        return content.Contains(StorageTransportPackage, StringComparison.Ordinal);
    }
}
