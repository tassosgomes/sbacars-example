using System.Diagnostics;
using AwesomeAssertions;
using SbaCars.TestKit;
using Xunit;

namespace SbaCars.Persistence.IntegrationTests;

/// <summary>
/// Closes the other §12 debt tracked since A4: <c>MigrationPipelineTests</c> proves the migration
/// pipeline by calling <c>InventoryDbContext.Database.MigrateAsync()</c> in-process, which is the
/// EF Core machinery the Migrator uses — but never actually runs
/// <c>SbaCars.Inventory.Migrator</c>'s own <c>Program.cs</c>: its own configuration binding
/// (<c>Persistence:ConnectionString</c> from environment variables), its own exit codes, its own
/// console output. That file was, until now, only ever verified by hand against the compose
/// Postgres (§12: "o binário só foi verificado à mão"). This launches the real built executable as
/// a child process — the same way the deploy pipeline will (§4.3, §11.5) — against the same
/// Testcontainers Postgres the rest of this project uses.
/// </summary>
/// <remarks>
/// Covers <c>SbaCars.Inventory.Migrator</c> as the representative case: all four services'
/// Migrators are generated from the exact same <c>Program.cs</c> shape (connection string from
/// config, <c>MigrateAsync</c>, exit 0/1) — see <c>SbaCars.Catalog.Migrator/Program.cs</c> for the
/// same structure with only the service name and <c>DbContext</c> type swapped. Running one for
/// real proves the shape; running all four would repeat the same proof three more times for the
/// same risk (per the testing skill's "qualidade do cenário prevalece sobre cobertura artificial").
/// </remarks>
[Collection(SbaCarsPostgresCollection.Name)]
public sealed class MigratorExecutableTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public MigratorExecutableTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RealMigratorExecutable_WithAValidConnectionString_AppliesMigrationsAndExitsZero()
    {
        var connectionString = _fixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw");

        var result = await RunMigratorAsync(connectionString);

        result.ExitCode.Should().Be(0, $"the migrator must exit 0 on success; stderr: {result.StandardError}");
        result.StandardOutput.Should().Contain(
            "Migrations applied successfully",
            "the real Program.cs, not just Database.MigrateAsync(), must have run to completion");
    }

    [Fact]
    public async Task RealMigratorExecutable_WithoutAConnectionString_ExitsOneWithoutThrowing()
    {
        var result = await RunMigratorAsync(connectionString: string.Empty);

        result.ExitCode.Should().Be(
            1, "a missing connection string must fail the process deliberately (exit 1), not crash unhandled");
        result.StandardError.Should().Contain(
            "Persistence:ConnectionString",
            "the executable's own validation message must explain what configuration is missing");
    }

    private static async Task<ProcessResult> RunMigratorAsync(string connectionString)
    {
        var migratorDllPath = MigratorExecutableLocator.Resolve("SbaCars.Inventory.Migrator");

        var startInfo = new ProcessStartInfo("dotnet", $"\"{migratorDllPath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // Overrides whatever appsettings.json/appsettings.Production.json ship next to the built
        // DLL — the same mechanism (Microsoft.Extensions.Configuration.EnvironmentVariables) the
        // real deploy pipeline uses to inject the schema-owning role's connection string (§4.3).
        startInfo.EnvironmentVariables["Persistence__ConnectionString"] = connectionString;
        startInfo.EnvironmentVariables["DOTNET_ENVIRONMENT"] = "Production";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process for '{migratorDllPath}'.");

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new ProcessResult(process.ExitCode, await stdOutTask, await stdErrTask);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
