using SbaCars.BuildingBlocks.Application;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Evidencias.GerarUrlUpload;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.UnitTests.Ofertas;

public sealed class GerarUrlUploadHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_OfertaInexistente_LancaOfertaNaoEncontradaException()
    {
        var handler = CreateHandler(
            oferta: null,
            objectStorage: new ThrowingObjectStorage(),
            unitOfWork: new FakeUnitOfWork());

        var act = () => handler.HandleAsync(
            new GerarUrlUploadCommand
            {
                OfertaId = Guid.CreateVersion7(),
                NomeArquivo = "laudo.pdf",
                TipoConteudo = "application/pdf",
                TamanhoBytes = 1024,
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<OfertaNaoEncontradaException>();
    }

    [Fact]
    public async Task HandleAsync_TamanhoExcedido_NaoChamaObjectStorage()
    {
        var oferta = CreateOferta();
        var objectStorage = new ThrowingObjectStorage();
        var handler = CreateHandler(oferta, objectStorage, new FakeUnitOfWork());

        var act = () => handler.HandleAsync(
            new GerarUrlUploadCommand
            {
                OfertaId = oferta.Id,
                NomeArquivo = "laudo.pdf",
                TipoConteudo = "application/pdf",
                TamanhoBytes = 11 * 1024 * 1024,
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ArquivoExcedeTamanhoException>();
        objectStorage.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_TipoConteudoInvalido_NaoChamaObjectStorage()
    {
        var oferta = CreateOferta();
        var objectStorage = new ThrowingObjectStorage();
        var handler = CreateHandler(oferta, objectStorage, new FakeUnitOfWork());

        var act = () => handler.HandleAsync(
            new GerarUrlUploadCommand
            {
                OfertaId = oferta.Id,
                NomeArquivo = "malware.exe",
                TipoConteudo = "application/x-msdownload",
                TamanhoBytes = 1024,
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TipoConteudoNaoAceitoException>();
        objectStorage.WasCalled.Should().BeFalse();
    }

  [Fact]
    public async Task HandleAsync_DadosValidos_RetornaUrlEGravaEvidencia()
    {
        var oferta = CreateOferta();
        var unitOfWork = new FakeUnitOfWork();
        var evidenciaRepository = new FakeEvidenciaRepository();
        var objectStorage = new StubObjectStorage();
        var handler = CreateHandler(oferta, objectStorage, unitOfWork, evidenciaRepository);

        var response = await handler.HandleAsync(
            new GerarUrlUploadCommand
            {
                OfertaId = oferta.Id,
                NomeArquivo = "laudo.pdf",
                TipoConteudo = "application/pdf",
                TamanhoBytes = 1024,
            },
            CancellationToken.None);

        unitOfWork.SaveCalls.Should().Be(1);
        evidenciaRepository.AddedCount.Should().Be(1);
        response.UploadUrl.Should().Be("https://storage.test/upload");
        response.HeadersObrigatorios.Should().ContainKey("Content-Type");
        response.EvidenciaId.Should().NotBe(Guid.Empty);
    }

    private static GerarUrlUploadHandler CreateHandler(
        Oferta? oferta,
        IObjectStorage objectStorage,
        FakeUnitOfWork unitOfWork,
        FakeEvidenciaRepository? evidenciaRepository = null) => new(
        new FakeOfertaRepository(oferta),
        evidenciaRepository ?? new FakeEvidenciaRepository(),
        objectStorage,
        new StubStorageSettings(),
        unitOfWork,
        new StubCurrentUser("operator-1", "Ana"),
        new FixedClock(Now));

    private static Oferta CreateOferta() => Oferta.Criar(
        new Veiculo(TipoVeiculo.CarroUsado, placa: "ABC1D23"),
        new Autoria("operator-1", "Ana", Now),
        Now);

    private sealed class FakeOfertaRepository(Oferta? oferta) : IOfertaRepository
    {
        public Task<Oferta?> ObterAsync(
            Guid ofertaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Oferta?>(oferta is not null && oferta.Id == ofertaId ? oferta : null);

        public Task<bool> ExistePlacaAtivaAsync(
            string placa,
            Guid? ignorarOfertaId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void Adicionar(Oferta entity) => throw new NotSupportedException();

        public void Remover(Oferta entity) => throw new NotSupportedException();
    }

    private sealed class FakeEvidenciaRepository : IEvidenciaRepository
    {
        public int AddedCount { get; private set; }

        public Task<Evidencia?> ObterAsync(
            Guid evidenciaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Evidencia?>(null);

        public Task<IReadOnlyList<Evidencia>> ObterVariosAsync(
            IEnumerable<Guid> evidenciaIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Evidencia>>([]);

        public void Adicionar(Evidencia evidencia) => AddedCount++;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class ThrowingObjectStorage : IObjectStorage
    {
        public bool WasCalled { get; private set; }

        public Task<ObjectStoragePresignedUrl> CreateUploadUrlAsync(
            string bucket,
            string key,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            throw new InvalidOperationException("Object storage should not be called.");
        }

        public Task<ObjectStoragePresignedUrl> CreateDownloadUrlAsync(
            string bucket,
            string key,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubObjectStorage : IObjectStorage
    {
        public Task<ObjectStoragePresignedUrl> CreateUploadUrlAsync(
            string bucket,
            string key,
            string contentType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ObjectStoragePresignedUrl(
                new Uri("https://storage.test/upload"),
                new Dictionary<string, string> { ["Content-Type"] = contentType },
                Now.AddMinutes(5)));

        public Task<ObjectStoragePresignedUrl> CreateDownloadUrlAsync(
            string bucket,
            string key,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubStorageSettings : IInventoryStorageSettings
    {
        public string BucketName => "sbacars-inventory-docs";
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class StubCurrentUser(string userId, string displayName) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public string? UserId => userId;

        public string? DisplayName => displayName;

        public IReadOnlyCollection<string> Permissions => ["estoque:gerenciar"];

        public bool HasPermission(string permission) => Permissions.Contains(permission);
    }
}
