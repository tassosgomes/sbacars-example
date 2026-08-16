namespace SbaCars.Architecture.Tests;

/// <summary>
/// Keeps the OpenTelemetry SDK out of the two layers the architecture plan says must stay clean
/// of framework dependency (§2.2): <c>Domain</c> has zero dependency on ASP.NET Core, EF Core or
/// any external SDK by design, and <c>Application</c> only knows abstractions
/// (<c>IUnitOfWork</c>, <c>IClock</c>, <c>ICurrentUser</c>) — never the concrete telemetry stack a
/// project's <c>Api</c>/<c>Infrastructure</c> wires. A8's own brief calls this out explicitly:
/// "Domain e Application não podem passar a depender do SDK do OpenTelemetry".
/// </summary>
public sealed class ObservabilityContainmentTests
{
    private static readonly string[] OpenTelemetryPackages =
    [
        "OpenTelemetry",
        "Npgsql.OpenTelemetry",
    ];

    [Fact]
    public void NoDomainOrApplicationProjectReferencesOpenTelemetry()
    {
        var offenders = Directory
            .EnumerateFiles(RepositoryPaths.BackendSrcDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(IsDomainOrApplicationProject)
            .Where(projectPath =>
            {
                var content = File.ReadAllText(projectPath);
                return OpenTelemetryPackages.Any(package =>
                    content.Contains(package, StringComparison.Ordinal));
            })
            .Select(Path.GetFileName)
            .ToArray();

        offenders.Should().BeEmpty(
            "Domain and Application must have zero dependency on the OpenTelemetry SDK (§2.2, §8) — " +
            "telemetry is wired in Api/Infrastructure only");
    }

    private static bool IsDomainOrApplicationProject(string projectPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(projectPath);
        return fileName.EndsWith(".Domain", StringComparison.Ordinal) ||
               fileName.EndsWith(".Application", StringComparison.Ordinal);
    }
}
