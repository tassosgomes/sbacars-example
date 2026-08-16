using DotNet.Testcontainers.Configurations;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace SbaCars.TestKit;

/// <summary>
/// One <c>postgres:18</c> container — the same major used by <c>docker-compose.yml</c> — bootstrapped
/// with the exact same init scripts under <c>backend/docker/postgres/init/</c> that provision the
/// local environment. Reusing the real scripts (rather than a trimmed-down copy for tests) is the
/// point: what proves the role/grant boundary in CI is what actually runs in Development.
/// </summary>
/// <remarks>
/// Moved into <c>SbaCars.TestKit</c> in A9, along with <see cref="SbaCarsPostgresCollection"/>.
/// Had exactly one consumer (<c>SbaCars.Persistence.IntegrationTests</c>) at the time of the move —
/// see <see cref="TestJwt"/>'s remarks for why that alone would not justify extraction under §3.3's
/// "second real consumer" rule, and why this moves anyway: §3.1 of the architecture plan names the
/// Testcontainers PostgreSQL fixture explicitly as TestKit content.
/// </remarks>
public sealed class SbaCarsPostgresFixture : IAsyncLifetime
{
    private const string AdminUsername = "postgres";
    private const string AdminPassword = "postgres";
    private const string DatabaseName = "sbacars";

    public PostgreSqlContainer Container { get; }

    public SbaCarsPostgresFixture()
    {
        Container = new PostgreSqlBuilder("postgres:18")
            .WithDatabase(DatabaseName)
            .WithUsername(AdminUsername)
            .WithPassword(AdminPassword)
            .WithBindMount(ResolveInitScriptsPath(), "/docker-entrypoint-initdb.d", AccessMode.ReadOnly)
            .Build();
    }

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();

    /// <summary>
    /// Connection string for an arbitrary role already provisioned by the init scripts (an
    /// <c>own_*</c> or <c>svc_*</c> role), against the shared <c>sbacars</c> database.
    /// </summary>
    public string ConnectionStringFor(string username, string password)
    {
        var builder = new NpgsqlConnectionStringBuilder(Container.GetConnectionString())
        {
            Username = username,
            Password = password,
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// Walks up from the test assembly's output directory until it finds
    /// <c>backend/docker/postgres/init</c>, so the fixture works regardless of the configuration
    /// (Debug/Release) or target framework folder the tests happen to run from.
    /// </summary>
    private static string ResolveInitScriptsPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "docker", "postgres", "init");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate 'docker/postgres/init' by walking up from '{AppContext.BaseDirectory}'.");
    }
}
