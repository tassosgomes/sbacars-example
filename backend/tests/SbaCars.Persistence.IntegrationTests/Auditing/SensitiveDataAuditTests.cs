using System.Diagnostics;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.BuildingBlocks.Persistence.Auditing;
using SbaCars.TestKit;
using Xunit;

namespace SbaCars.Persistence.IntegrationTests.Auditing;

/// <summary>
/// Proves the read-access audit trail required by §5.7 of the foundation plan (A4b's readiness
/// criterion): reading a record marked <see cref="ISensitiveDataEntity"/> produces an audit row
/// carrying who, what, when and the request's correlation id; reading an unmarked record produces
/// none. Runs against its own <c>postgres:18</c> container (<see cref="IClassFixture{TFixture}"/>,
/// not the shared collection) because, unlike the other integration tests in this project, these
/// tests assert an *exact* row count in a table — sharing a database with tests that only ever add
/// unrelated tables would make that assertion depend on run order.
/// </summary>
public sealed class SensitiveDataAuditTests : IClassFixture<SbaCarsPostgresFixture>, IAsyncLifetime
{
    private const string Schema = "catalog";
    private static readonly ActivitySource TestActivitySource = new("SbaCars.Persistence.IntegrationTests.Auditing");

    private readonly SbaCarsPostgresFixture _fixture;
    private readonly ActivityListener _activityListener;

    public SensitiveDataAuditTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;

        // Activity.Current stays null with no listener attached anywhere in the process — this
        // test needs a real (non-null) trace id to assert the audit row's CorrelationId against.
        _activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    public async Task InitializeAsync()
    {
        var ownerConnectionString = _fixture.ConnectionStringFor("own_catalog", "own_catalog_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<AuditingProbeDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(ownerConnectionString, Schema);

        await using var context = new AuditingProbeDbContext(optionsBuilder.Options, Schema, sensitiveDataAuditInterceptor: null);
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        _activityListener.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ReadingASensitiveEntity_RecordsWhoWhatWhenAndCorrelationId()
    {
        var currentUser = new FakeCurrentUser { UserId = "operador-42" };
        var clock = new FakeClock();
        var interceptor = new SensitiveDataAuditInterceptor(currentUser, clock);

        var appConnectionString = _fixture.ConnectionStringFor("svc_catalog", "svc_catalog_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<AuditingProbeDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(appConnectionString, Schema).AddInterceptors(interceptor);

        var entity = new SensitiveProbeEntity("dossie-de-joao");

        await using (var writeContext = new AuditingProbeDbContext(optionsBuilder.Options, Schema, interceptor))
        {
            writeContext.SensitiveProbes.Add(entity);
            await writeContext.SaveChangesAsync();
        }

        using var activity = TestActivitySource.StartActivity("audit-test-read");
        activity.Should().NotBeNull("an ActivityListener was registered so a real trace id exists to assert against");

        await using (var readContext = new AuditingProbeDbContext(optionsBuilder.Options, Schema, interceptor))
        {
            var repository = new SensitiveProbeRepository(readContext);
            var reloaded = await repository.FindAsync(entity.Id);
            reloaded.Should().NotBeNull();
        }

        await using var verifyContext = new AuditingProbeDbContext(optionsBuilder.Options, Schema, sensitiveDataAuditInterceptor: null);
        var readAudits = await verifyContext.AuditEntries
            .Where(a => a.EntityId == entity.Id && a.AccessType == SensitiveDataAccessType.Read)
            .ToListAsync();

        readAudits.Should().HaveCount(1, "one FindAsync call materializing one sensitive row must produce exactly one read audit entry");

        var readAudit = readAudits[0];
        readAudit.EntityName.Should().Be(nameof(SensitiveProbeEntity), "the audit row must say *what* was read");
        readAudit.UserId.Should().Be("operador-42", "the audit row must say *who* read it, from ICurrentUser");
        readAudit.OccurredAt.Should().Be(clock.UtcNow, "the audit row's timestamp must come from IClock, never DateTime.UtcNow directly");
        readAudit.CorrelationId.Should().Be(activity!.Id, "the audit row must carry the request's trace id, to tie it to the log line and the trace of the same request");
    }

    [Fact]
    public async Task ReadingAnUnmarkedEntity_ProducesNoAuditRow()
    {
        var currentUser = new FakeCurrentUser();
        var clock = new FakeClock();
        var interceptor = new SensitiveDataAuditInterceptor(currentUser, clock);

        var appConnectionString = _fixture.ConnectionStringFor("svc_catalog", "svc_catalog_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<AuditingProbeDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(appConnectionString, Schema).AddInterceptors(interceptor);

        var plainEntity = new ProbeEntity("catalogo-publico");

        await using (var writeContext = new AuditingProbeDbContext(optionsBuilder.Options, Schema, interceptor))
        {
            writeContext.PlainProbes.Add(plainEntity);
            await writeContext.SaveChangesAsync();
        }

        await using (var readContext = new AuditingProbeDbContext(optionsBuilder.Options, Schema, interceptor))
        {
            var repository = new PlainProbeRepository(readContext);
            var reloaded = await repository.FindAsync(plainEntity.Id);
            reloaded.Should().NotBeNull();
        }

        await using var verifyContext = new AuditingProbeDbContext(optionsBuilder.Options, Schema, sensitiveDataAuditInterceptor: null);
        var anyAuditForThisEntity = await verifyContext.AuditEntries
            .AnyAsync(a => a.EntityId == plainEntity.Id);

        anyAuditForThisEntity.Should().BeFalse(
            "ProbeEntity does not implement ISensitiveDataEntity — reading it must not generate audit noise");
    }
}
