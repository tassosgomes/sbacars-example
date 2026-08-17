using SbaCars.BuildingBlocks.Application.Cqrs;

namespace SbaCars.Inventory.Application.Evidencias.GerarUrlUpload;

public sealed record GerarUrlUploadCommand : ICommand<UploadEvidenciaResponse>
{
    public Guid OfertaId { get; init; }

    public string NomeArquivo { get; init; } = string.Empty;

    public string TipoConteudo { get; init; } = string.Empty;

    public long TamanhoBytes { get; init; }
}
