using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.BuildingBlocks.Persistence.Auditing;
using SbaCars.BuildingBlocks.Web.Auditing;
using SbaCars.TestKit;
using Xunit;

namespace SbaCars.Persistence.IntegrationTests.Auditing;

/// <summary>
/// Proves the A6b readiness criterion (§12 of the foundation plan): a request that only reads a
/// sensitive entity — never calling <c>SaveChanges</c>, and never going through
/// <see cref="Repository{TEntity}.FindAsync"/>'s own flush — still ends up with a persisted audit
/// row, because <c>SensitiveDataAuditFlushMiddleware</c> (<c>BuildingBlocks.Web</c>) flushes it at
/// the end of the request regardless. Every test here reads via a raw <c>DbSet</c> LINQ query
/// (never through <see cref="Repository{TEntity}"/>) specifically so the middleware — not the
/// repository's own flush call already proven by <see cref="SensitiveDataAuditTests"/> — is what
/// is under test.
/// </summary>
public sealed class SensitiveDataAuditFlushMiddlewareTests : IClassFixture<SbaCarsPostgresFixture>, IAsyncLifetime
{
    // Same schema SensitiveDataAuditTests uses for this very same AuditingProbeDbContext type —
    // no longer load-bearing since A9's SchemaAwareModelCacheKeyFactory keys the compiled-model
    // cache by schema as well as CLR type (see ModelCacheKeyFactoryTests for the regression proof
    // with two distinct schemas). Left matching anyway: there is no reason for this class to pick
    // a different one, and it keeps both classes' seeded data in the same schema.
    private const string Schema = "catalog";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SbaCarsPostgresFixture _fixture;
    private readonly ActivityListener _activityListener;

    public SensitiveDataAuditFlushMiddlewareTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;

        // Same reasoning as SensitiveDataAuditTests: ASP.NET Core only creates a real
        // Activity.Current for a request when something is listening for it.
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
    public async Task ReadOnlyRequest_WithNoSaveChanges_StillPersistsAuditRow()
    {
        var entity = new SensitiveProbeEntity("dossie-lido-sem-savechanges");
        await SeedAsync(entity);

        await using var host = await AuditHost.StartAsync(_fixture, Schema, new FakeCurrentUser { UserId = "operador-42" }, new FakeClock());

        var response = await host.Client.GetAsync($"/sensitive/{entity.Id}");
        response.EnsureSuccessStatusCode();

        // The server-side trace id, read back from the response the endpoint deliberately echoes
        // (Activity.Current?.Id captured *inside* the request) — not one manufactured by the test
        // itself: TestServer's in-memory HttpClient does not run the normal DiagnosticsHandler
        // pipeline that would propagate a client-side Activity's traceparent into the request, so
        // the request gets its own, ASP.NET Core-hosting-created Activity server-side.
        var payload = await response.Content.ReadFromJsonAsync<SensitiveReadResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.TraceId.Should().NotBeNullOrEmpty("an ActivityListener was registered so a real trace id exists to assert against");

        var readAudits = await ReadAuditEntriesAsync(entity.Id, SensitiveDataAccessType.Read);

        readAudits.Should().HaveCount(1,
            "a GET that only reads a sensitive entity — no SaveChanges anywhere in the request — must still " +
            "produce exactly one read audit row once the end-of-request middleware flushes it");
        readAudits[0].UserId.Should().Be("operador-42");
        readAudits[0].CorrelationId.Should().Be(payload.TraceId,
            "the audit row must carry the same trace id as the request that produced it");
    }

    [Fact]
    public async Task RequestThatThrows_StillPersistsWhatWasRead()
    {
        var entity = new SensitiveProbeEntity("dossie-lido-antes-de-explodir");
        await SeedAsync(entity);

        await using var host = await AuditHost.StartAsync(_fixture, Schema, new FakeCurrentUser { UserId = "operador-99" }, new FakeClock());

        var response = await host.Client.GetAsync($"/sensitive-then-throw/{entity.Id}");

        response.IsSuccessStatusCode.Should().BeFalse("the endpoint deliberately throws after reading");

        var readAudits = await ReadAuditEntriesAsync(entity.Id, SensitiveDataAccessType.Read);

        readAudits.Should().HaveCount(1,
            "the request threw after reading the entity, but the middleware's flush runs in a finally block " +
            "nested inside the exception handler, so what was read before the failure must still be audited");
    }

    [Fact]
    public async Task FlushFailure_DoesNotFailASuccessfulResponse_ButIsLogged()
    {
        var entity = new SensitiveProbeEntity("dossie-com-flush-quebrado");
        await SeedAsync(entity);

        var logSink = new CapturingLoggerProvider();
        await using var host = await AuditHost.StartAsync(
            _fixture,
            Schema,
            new FakeCurrentUser { UserId = "operador-7" },
            new FakeClock(),
            logSink,
            useThrowingFlusher: true);

        var response = await host.Client.GetAsync($"/sensitive/{entity.Id}");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SensitiveReadResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Name.Should().Be(entity.Name, "a broken audit flush must not turn a successful read into an error response");

        logSink.Entries.Should().Contain(
            e => e.Level == LogLevel.Error && e.Exception is InvalidOperationException,
            "a failed flush must not fail silently — it has to be logged so a broken audit pipeline is an operable incident");
    }

    [Fact]
    public async Task ReadingAnUnmarkedEntity_ThroughTheMiddleware_ProducesNoAuditRow()
    {
        var plainEntity = new ProbeEntity("catalogo-publico-via-middleware");
        await SeedAsync(plainEntity);

        await using var host = await AuditHost.StartAsync(_fixture, Schema, new FakeCurrentUser(), new FakeClock());

        var response = await host.Client.GetAsync($"/plain/{plainEntity.Id}");
        response.EnsureSuccessStatusCode();

        var ownerConnectionString = _fixture.ConnectionStringFor("own_catalog", "own_catalog_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<AuditingProbeDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(ownerConnectionString, Schema);
        await using var verifyContext = new AuditingProbeDbContext(optionsBuilder.Options, Schema, sensitiveDataAuditInterceptor: null);

        var anyAuditForThisEntity = await verifyContext.AuditEntries.AnyAsync(a => a.EntityId == plainEntity.Id);

        anyAuditForThisEntity.Should().BeFalse(
            "ProbeEntity is not ISensitiveDataEntity — the middleware's flush must stay a harmless no-op for it");
    }

    private async Task SeedAsync(SensitiveProbeEntity entity)
    {
        var interceptor = new SensitiveDataAuditInterceptor(new FakeCurrentUser(), new FakeClock());
        var appConnectionString = _fixture.ConnectionStringFor("svc_catalog", "svc_catalog_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<AuditingProbeDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(appConnectionString, Schema).AddInterceptors(interceptor);

        await using var writeContext = new AuditingProbeDbContext(optionsBuilder.Options, Schema, interceptor);
        writeContext.SensitiveProbes.Add(entity);
        await writeContext.SaveChangesAsync();
    }

    private async Task SeedAsync(ProbeEntity entity)
    {
        var appConnectionString = _fixture.ConnectionStringFor("svc_catalog", "svc_catalog_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<AuditingProbeDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(appConnectionString, Schema);

        await using var writeContext = new AuditingProbeDbContext(optionsBuilder.Options, Schema, sensitiveDataAuditInterceptor: null);
        writeContext.PlainProbes.Add(entity);
        await writeContext.SaveChangesAsync();
    }

    private async Task<List<SensitiveDataAuditEntry>> ReadAuditEntriesAsync(Guid entityId, SensitiveDataAccessType accessType)
    {
        var ownerConnectionString = _fixture.ConnectionStringFor("own_catalog", "own_catalog_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<AuditingProbeDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(ownerConnectionString, Schema);

        await using var verifyContext = new AuditingProbeDbContext(optionsBuilder.Options, Schema, sensitiveDataAuditInterceptor: null);
        return await verifyContext.AuditEntries
            .Where(a => a.EntityId == entityId && a.AccessType == accessType)
            .ToListAsync();
    }

    /// <summary>
    /// A minimal in-process host (<see cref="TestServer"/>) that mirrors the slice of the four
    /// real services' <c>Program.cs</c> this test needs: an exception handler, then
    /// <c>SensitiveDataAuditFlushMiddleware</c>, then endpoints reading straight off a
    /// request-scoped <see cref="AuditingProbeDbContext"/> without going through
    /// <see cref="Repository{TEntity}"/>.
    /// </summary>
    private sealed class AuditHost : IAsyncDisposable
    {
        private readonly WebApplication _app;

        public HttpClient Client { get; }

        private AuditHost(WebApplication app, HttpClient client)
        {
            _app = app;
            Client = client;
        }

        public static async Task<AuditHost> StartAsync(
            SbaCarsPostgresFixture fixture,
            string schema,
            ICurrentUser currentUser,
            IClock clock,
            ILoggerProvider? logSink = null,
            bool useThrowingFlusher = false)
        {
            var appConnectionString = fixture.ConnectionStringFor("svc_catalog", "svc_catalog_dev_pw");

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            if (logSink is not null)
            {
                builder.Logging.AddProvider(logSink);
            }

            builder.Services.AddScoped<ICurrentUser>(_ => currentUser);
            builder.Services.AddSingleton(clock);
            builder.Services.AddScoped<SensitiveDataAuditInterceptor>();

            // AuditingProbeDbContext takes its schema as a constructor argument (it targets either
            // "catalog" or "inventory" in other tests in this project), so — unlike a real
            // service's own DbContext, which hardcodes its schema — it needs a factory instead of
            // plain `AddDbContext<T>` constructor injection.
            builder.Services.AddScoped(provider =>
            {
                var interceptor = provider.GetRequiredService<SensitiveDataAuditInterceptor>();
                var optionsBuilder = new DbContextOptionsBuilder<AuditingProbeDbContext>();
                optionsBuilder.UseSbaCarsNpgsql(appConnectionString, schema).AddInterceptors(interceptor);
                return new AuditingProbeDbContext(optionsBuilder.Options, schema, interceptor);
            });

            if (useThrowingFlusher)
            {
                builder.Services.AddScoped<ISensitiveDataAuditFlusher, ThrowingSensitiveDataAuditFlusher>();
            }
            else
            {
                builder.Services.AddSbaCarsSensitiveDataAuditFlusher<AuditingProbeDbContext>();
            }

            var app = builder.Build();

            // Mirrors the real Program.cs: UseExceptionHandler must wrap the flush middleware so a
            // downstream throw still reaches the flush's `finally` before becoming a response.
            app.Use(async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (Exception)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            });

            app.UseSbaCarsSensitiveDataAuditFlush();

            app.MapGet("/sensitive/{id:guid}", async (Guid id, AuditingProbeDbContext db, CancellationToken ct) =>
            {
                var entity = await db.SensitiveProbes.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
                return entity is null
                    ? Results.NotFound()
                    : Results.Ok(new SensitiveReadResponse(entity.Name, Activity.Current?.Id));
            });

            app.MapGet("/plain/{id:guid}", async (Guid id, AuditingProbeDbContext db, CancellationToken ct) =>
            {
                var entity = await db.PlainProbes.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
                return entity is null ? Results.NotFound() : Results.Ok(entity.Name);
            });

            app.MapGet("/sensitive-then-throw/{id:guid}", async (Guid id, AuditingProbeDbContext db, CancellationToken ct) =>
            {
                await db.SensitiveProbes.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
                throw new InvalidOperationException("Deliberate failure after a sensitive read, for test purposes.");
            });

            await app.StartAsync();

            return new AuditHost(app, app.GetTestClient());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// What <c>/sensitive/{id}</c> returns: the entity's name (proving the read succeeded) and the
    /// server-side trace id captured from <see cref="Activity.Current"/> while handling the
    /// request — the value tests compare against the audit row's <c>CorrelationId</c>.
    /// </summary>
    private sealed record SensitiveReadResponse(string Name, string? TraceId);

    private sealed class ThrowingSensitiveDataAuditFlusher : ISensitiveDataAuditFlusher
    {
        public Task FlushAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Deliberate audit flush failure, for test purposes.");
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(CapturingLoggerProvider owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                lock (owner.Entries)
                {
                    owner.Entries.Add((logLevel, formatter(state, exception), exception));
                }
            }
        }
    }
}
