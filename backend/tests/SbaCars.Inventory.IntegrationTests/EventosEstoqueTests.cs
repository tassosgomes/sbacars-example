using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Pipeline;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Messaging;
using SbaCars.BuildingBlocks.Messaging.Tracing;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Contracts.Estoque.V1;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Application.Ofertas.AlterarDisponibilidade;
using SbaCars.Inventory.Application.Solicitacoes.AprovarSolicitacao;
using SbaCars.Inventory.Application.Solicitacoes.RejeitarSolicitacao;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;
using SbaCars.Inventory.Infrastructure;
using SbaCars.Inventory.Infrastructure.Ofertas;
using SbaCars.Inventory.Infrastructure.Solicitacoes;
using SbaCars.TestKit;

namespace SbaCars.Inventory.IntegrationTests;

/// <summary>
/// V-09 proof against the real inventory schema, Rebus PostgreSQL outbox and RabbitMQ broker.
/// </summary>
[Collection(InventoryRabbitMqCollection.Name)]
public sealed class EventosEstoqueTests : IClassFixture<SbaCarsPostgresFixture>
{
    private static readonly DateTimeOffset TestNow =
        new(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);

    private readonly SbaCarsRabbitMqFixture _rabbitMqFixture;
    private readonly SbaCarsPostgresFixture _postgresFixture;

    public EventosEstoqueTests(
        SbaCarsRabbitMqFixture rabbitMqFixture,
        SbaCarsPostgresFixture postgresFixture)
    {
        _rabbitMqFixture = rabbitMqFixture;
        _postgresFixture = postgresFixture;
    }

    [Fact]
    public async Task AprovarElegibilidade_PublicaContratoIncluidoComTraceparent()
    {
        await EnsureSchemaAsync();
        InventoryEventHandler.Reset();

        var oferta = CreateCompleteOffer(SituacaoOferta.EmPreparacao, "V09A123");
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Elegibilidade);
        await PersistAsync(oferta, solicitacao);

        var queueName = UniqueQueueName("inventory-v09-included");
        await using var host = await StartHostAsync(queueName);
        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<OfertaIncluidaIntegrationEvent>();

        var exportedActivities = new List<Activity>();
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(MessagingActivitySource.Name)
            .AddInMemoryExporter(exportedActivities)
            .Build();
        using var rootActivity = MessagingActivitySource.Instance.StartActivity(
            "inventory-v09-approval-root",
            ActivityKind.Internal);
        rootActivity.Should().NotBeNull("the test root must be sampled so the outbox traceparent can be asserted");

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var handler = CreateApprovalHandler(scope.ServiceProvider, TestNow.AddHours(2));
            await handler.HandleAsync(
                new AprovarSolicitacaoCommand { SolicitacaoId = solicitacao.Id },
                CancellationToken.None);
        }

        var published = await InventoryEventHandler.WaitForIncludedAsync(TimeSpan.FromSeconds(15));
        published.Should().NotBeNull("the committed outbox message must reach RabbitMQ");
        published!.OfertaId.Should().Be(oferta.Id);
        published.OcorridoEm.Should().Be(TestNow.AddHours(2));
        InventoryEventHandler.ObservedTraceparent.Should().NotBeNullOrWhiteSpace();
        InventoryEventHandler.ObservedTraceparent.Should().Contain(rootActivity!.TraceId.ToHexString());
    }

    [Fact]
    public async Task AlterarDisponibilidade_PublicaEstadoCanonicoSemAcento()
    {
        await EnsureSchemaAsync();
        InventoryEventHandler.Reset();

        var oferta = Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroUsado, placa: "V09B456"),
            new Autoria("operator-1", "Ana", TestNow),
            TestNow);
        await PersistAsync(oferta);

        var queueName = UniqueQueueName("inventory-v09-availability");
        await using var host = await StartHostAsync(queueName);
        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<DisponibilidadeAlteradaIntegrationEvent>();

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var handler = CreateAvailabilityHandler(scope.ServiceProvider, TestNow.AddMinutes(5));
            await handler.HandleAsync(
                new AlterarDisponibilidadeCommand
                {
                    OfertaId = oferta.Id,
                    NovoEstado = EstadoDisponibilidade.Reservado,
                    Observacao = "Reserva operacional.",
                },
                CancellationToken.None);
        }

        var published = await InventoryEventHandler.WaitForAvailabilityAsync(TimeSpan.FromSeconds(15));
        published.Should().NotBeNull("a direct availability transition must reach RabbitMQ");
        published!.OfertaId.Should().Be(oferta.Id);
        published.Disponibilidade.Should().Be("reservado");
    }

    [Fact]
    public async Task FalhaDepoisDaMutacao_NaoPublicaENaoPersisteOfertaOuSolicitacao()
    {
        await EnsureSchemaAsync();
        InventoryEventHandler.Reset();

        var oferta = CreateCompleteOffer(SituacaoOferta.EmPreparacao, "V09C789");
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Elegibilidade);
        await PersistAsync(oferta, solicitacao);

        var outboxBefore = await CountOutboxRowsAsync();
        var queueName = UniqueQueueName("inventory-v09-rollback");
        await using var host = await StartHostAsync(queueName);
        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<OfertaIncluidaIntegrationEvent>();

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var handler = CreateApprovalHandler(
                scope.ServiceProvider,
                TestNow.AddHours(3),
                new ThrowingUnitOfWork());

            var act = () => handler.HandleAsync(
                new AprovarSolicitacaoCommand { SolicitacaoId = solicitacao.Id },
                CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        var published = await InventoryEventHandler.WaitForIncludedAsync(TimeSpan.FromSeconds(3));
        published.Should().BeNull("a staged event must be discarded when the unit of work fails");
        (await CountOutboxRowsAsync()).Should().Be(outboxBefore);

        await using var read = CreateContext();
        var persistedOffer = await read.Ofertas.AsNoTracking().SingleAsync(item => item.Id == oferta.Id);
        var persistedRequest = await read.Solicitacoes.AsNoTracking().SingleAsync(item => item.Id == solicitacao.Id);
        persistedOffer.Situacao.Should().Be(SituacaoOferta.EmPreparacao);
        persistedRequest.Status.Should().Be(StatusSolicitacao.Pendente);
    }

    [Fact]
    public async Task RejeitarSolicitacao_NaoPublicaEvento()
    {
        await EnsureSchemaAsync();
        InventoryEventHandler.Reset();

        var oferta = CreateCompleteOffer(SituacaoOferta.Elegivel, "V09D012");
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Retirada);
        await PersistAsync(oferta, solicitacao);

        var outboxBefore = await CountOutboxRowsAsync();
        var queueName = UniqueQueueName("inventory-v09-rejected");
        await using var host = await StartHostAsync(queueName);

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var handler = new RejeitarSolicitacaoHandler(
                scope.ServiceProvider.GetRequiredService<ISolicitacaoRepository>(),
                scope.ServiceProvider.GetRequiredService<IOfertaRepository>(),
                scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                new StubCurrentUser("validator-1", "Bruno"),
                new FixedClock(TestNow.AddHours(1)),
                new CalculadoraDiasUteis(),
                scope.ServiceProvider.GetRequiredService<ILogger<RejeitarSolicitacaoHandler>>());

            await handler.HandleAsync(
                new RejeitarSolicitacaoCommand
                {
                    SolicitacaoId = solicitacao.Id,
                    Justificativa = "Retirada não aprovada.",
                },
                CancellationToken.None);
        }

        (await CountOutboxRowsAsync()).Should().Be(outboxBefore);
        await using var read = CreateContext();
        var persistedOffer = await read.Ofertas.AsNoTracking().SingleAsync(item => item.Id == oferta.Id);
        var persistedRequest = await read.Solicitacoes.AsNoTracking().SingleAsync(item => item.Id == solicitacao.Id);
        persistedOffer.Situacao.Should().Be(SituacaoOferta.Elegivel);
        persistedRequest.Status.Should().Be(StatusSolicitacao.Rejeitada);
        (await InventoryEventHandler.WaitForIncludedAsync(TimeSpan.FromSeconds(1))).Should().BeNull();
    }

    private async Task<InventoryMessagingHost> StartHostAsync(string queueName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:ConnectionString"] = _rabbitMqFixture.AmqpConnectionString,
                ["Messaging:InputQueueName"] = queueName,
                ["Persistence:ConnectionString"] =
                    _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"),
            })
            .Build();

        return await InventoryMessagingHost.StartAsync(services =>
        {
            services.AddLogging(builder => builder.AddDebug());
            services.AddSingleton<CalculadoraDiasUteis>();
            services.AddInventoryInfrastructure(configuration);
            services.AddSbaCarsMessaging(configuration, "inventory-v09", InventoryDbContext.Schema);
            services.AddTransient<IHandleMessages<OfertaIncluidaIntegrationEvent>, InventoryEventHandler>();
            services.AddTransient<IHandleMessages<OfertaAtualizadaIntegrationEvent>, InventoryEventHandler>();
            services.AddTransient<IHandleMessages<OfertaRetiradaIntegrationEvent>, InventoryEventHandler>();
            services.AddTransient<IHandleMessages<DisponibilidadeAlteradaIntegrationEvent>, InventoryEventHandler>();
        });
    }

    private AprovarSolicitacaoHandler CreateApprovalHandler(
        IServiceProvider services,
        DateTimeOffset decidedAt,
        IUnitOfWork? unitOfWork = null) =>
        new(
            services.GetRequiredService<ISolicitacaoRepository>(),
            services.GetRequiredService<IOfertaRepository>(),
            unitOfWork ?? services.GetRequiredService<IUnitOfWork>(),
            services.GetRequiredService<IEstoqueIntegrationEventPublisher>(),
            new StubCurrentUser("validator-1", "Bruno"),
            new FixedClock(decidedAt),
            new CalculadoraDiasUteis(),
            services.GetRequiredService<ILogger<AprovarSolicitacaoHandler>>());

    private AlterarDisponibilidadeCommandHandler CreateAvailabilityHandler(
        IServiceProvider services,
        DateTimeOffset changedAt) =>
        new(
            services.GetRequiredService<IOfertaRepository>(),
            services.GetRequiredService<IUnitOfWork>(),
            services.GetRequiredService<IEstoqueIntegrationEventPublisher>(),
            new StubCurrentUser("operator-2", "Bruno"),
            new FixedClock(changedAt),
            services.GetRequiredService<ILogger<AlterarDisponibilidadeCommandHandler>>());

    private async Task EnsureSchemaAsync()
    {
        await using var context = CreateContext(
            _postgresFixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw"));
        await context.Database.MigrateAsync();
    }

    private async Task PersistAsync(Oferta oferta, Solicitacao? solicitacao = null)
    {
        await using var context = CreateContext();
        context.Ofertas.Add(oferta);
        if (solicitacao is not null)
        {
            context.Solicitacoes.Add(solicitacao);
        }

        await context.SaveChangesAsync();
    }

    private async Task<long> CountOutboxRowsAsync()
    {
        await using var connection = new NpgsqlConnection(
            _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM inventory.outbox;";
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private InventoryDbContext CreateContext() => CreateContext(
        _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));

    private static InventoryDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        options.UseSbaCarsNpgsql(connectionString, InventoryDbContext.Schema);
        return new InventoryDbContext(options.Options);
    }

    private static Oferta CreateCompleteOffer(SituacaoOferta situacao, string plate)
    {
        var autoria = new Autoria("operator-1", "Ana", TestNow);
        var oferta = Oferta.Criar(
            new Veiculo(
                TipoVeiculo.CarroSeminovo,
                placa: plate,
                marca: "Honda",
                modelo: "Civic",
                versao: "EXL",
                anoFabricacao: 2021,
                anoModelo: 2022,
                quilometragem: 48_300,
                cambio: "Automático",
                localizacao: new Localizacao("13010-111", "Campinas", "SP")),
            autoria,
            TestNow);

        SetProperty(oferta, nameof(Oferta.Fatos), FatosConhecidos.Criar(
            new BlocoFato(BlocoFatoTipo.Origem, descricao: "Origem conhecida", atualizadoPor: autoria),
            new BlocoFato(BlocoFatoTipo.Condicao, descricao: "Condição conhecida", atualizadoPor: autoria),
            new BlocoFato(BlocoFatoTipo.Historico, descricao: "Histórico conhecido", atualizadoPor: autoria)));
        oferta.DefinirPrecoInicial(8_790_000, autoria.UsuarioId, autoria.Nome, TestNow);
        SetProperty(oferta.Disponibilidade, nameof(Disponibilidade.EstadoConhecido), true);
        SetProperty(oferta, nameof(Oferta.Situacao), situacao);
        return oferta;
    }

    private static Solicitacao CreateRequest(Oferta oferta, TipoSolicitacao tipo) => Solicitacao.Abrir(
        oferta.Id,
        tipo,
        null,
        "Solicitação operacional.",
        new Autoria("operator-1", "Ana", TestNow),
        TestNow);

    private static string UniqueQueueName(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 13, 40)];

    private static void SetProperty<T>(T target, string propertyName, object? value) =>
        typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);

    private sealed class StubCurrentUser(string userId, string displayName) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public string? UserId => userId;

        public string? DisplayName => displayName;

        public IReadOnlyCollection<string> Permissions => ["estoque:validar", "estoque:gerenciar"];

        public bool HasPermission(string permission) => Permissions.Contains(permission);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class ThrowingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<int>(new InvalidOperationException("forced V-09 rollback"));
    }
}

[CollectionDefinition(Name)]
public sealed class InventoryRabbitMqCollection : ICollectionFixture<SbaCarsRabbitMqFixture>
{
    public const string Name = "inventory-rabbitmq";
}

internal sealed class InventoryMessagingHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    private InventoryMessagingHost(ServiceProvider provider)
    {
        _provider = provider;
    }

    public IServiceProvider Services => _provider;

    public static async Task<InventoryMessagingHost> StartAsync(
        Action<IServiceCollection> configureServices,
        CancellationToken cancellationToken = default)
    {
        var services = new ServiceCollection();
        configureServices(services);
        var provider = services.BuildServiceProvider();

        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(cancellationToken);
        }

        return new InventoryMessagingHost(provider);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var hostedService in _provider.GetServices<IHostedService>())
        {
            await hostedService.StopAsync(CancellationToken.None);
        }

        await _provider.DisposeAsync();
    }
}

public sealed class InventoryEventHandler :
    IHandleMessages<OfertaIncluidaIntegrationEvent>,
    IHandleMessages<OfertaAtualizadaIntegrationEvent>,
    IHandleMessages<OfertaRetiradaIntegrationEvent>,
    IHandleMessages<DisponibilidadeAlteradaIntegrationEvent>
{
    private static TaskCompletionSource<OfertaIncluidaIntegrationEvent> _included = CreateSource<OfertaIncluidaIntegrationEvent>();
    private static TaskCompletionSource<DisponibilidadeAlteradaIntegrationEvent> _availability = CreateSource<DisponibilidadeAlteradaIntegrationEvent>();

    public static string? ObservedTraceparent { get; private set; }

    public static void Reset()
    {
        _included = CreateSource<OfertaIncluidaIntegrationEvent>();
        _availability = CreateSource<DisponibilidadeAlteradaIntegrationEvent>();
        ObservedTraceparent = null;
    }

    public Task Handle(OfertaIncluidaIntegrationEvent message)
    {
        CaptureTraceparent();
        _included.TrySetResult(message);
        return Task.CompletedTask;
    }

    public Task Handle(OfertaAtualizadaIntegrationEvent message) => Task.CompletedTask;

    public Task Handle(OfertaRetiradaIntegrationEvent message) => Task.CompletedTask;

    public Task Handle(DisponibilidadeAlteradaIntegrationEvent message)
    {
        _availability.TrySetResult(message);
        return Task.CompletedTask;
    }

    public static Task<OfertaIncluidaIntegrationEvent?> WaitForIncludedAsync(TimeSpan timeout) =>
        WaitAsync(_included.Task, timeout);

    public static Task<DisponibilidadeAlteradaIntegrationEvent?> WaitForAvailabilityAsync(TimeSpan timeout) =>
        WaitAsync(_availability.Task, timeout);

    private static void CaptureTraceparent()
    {
        var headers = MessageContext.Current?.TransportMessage.Headers;
        ObservedTraceparent = headers?.GetValueOrDefault("traceparent");
    }

    private static async Task<T?> WaitAsync<T>(Task<T> task, TimeSpan timeout)
    {
        try
        {
            return await task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            return default;
        }
    }

    private static TaskCompletionSource<T> CreateSource<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
