namespace SbaCars.Architecture.Tests;

/// <summary>
/// Keeps the reverse proxy inside the edge. Only the two gateways — and the library they share,
/// <c>SbaCars.Gateway.Shared</c> — may depend on <c>Yarp.ReverseProxy</c>. A domain service that
/// carries it, even transitively through <c>BuildingBlocks.Web</c>, is a service shipping the
/// machinery for calling another service synchronously, which §2.4 of the architecture plan
/// forbids: every dependency between domains is an asynchronous integration event.
/// </summary>
/// <remarks>
/// This started as a real regression, not a hypothetical: A7 first placed the shared YARP wiring
/// in <c>BuildingBlocks.Web</c>, which every <c>Api</c> references, and
/// <c>Yarp.ReverseProxy.dll</c> duly appeared in all four services' build output. Scanning
/// <c>.csproj</c> text (rather than reflecting over assemblies) catches the direct reference at
/// its source, which is the only place the mistake can be made.
/// </remarks>
public sealed class ReverseProxyContainmentTests
{
    private const string ReverseProxyPackage = "Yarp.ReverseProxy";

    [Fact]
    public void OnlyGatewayProjectsReferenceYarp()
    {
        var offenders = Directory
            .EnumerateFiles(RepositoryPaths.BackendSrcDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(projectPath => File.ReadAllText(projectPath).Contains(ReverseProxyPackage, StringComparison.Ordinal))
            .Where(projectPath => !IsUnderGateways(projectPath))
            .Select(Path.GetFileName)
            .ToArray();

        offenders.Should().BeEmpty(
            $"only projects under src/Gateways/ may reference {ReverseProxyPackage} (§2.4, §3.3)");
    }

    private static bool IsUnderGateways(string projectPath) =>
        projectPath.Contains(
            Path.Combine(RepositoryPaths.BackendSrcDirectory, "Gateways") + Path.DirectorySeparatorChar,
            StringComparison.Ordinal);
}
