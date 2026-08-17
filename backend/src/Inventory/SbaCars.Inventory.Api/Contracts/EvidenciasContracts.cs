using SbaCars.Inventory.Application.Evidencias.GerarUrlDownload;
using SbaCars.Inventory.Application.Evidencias.GerarUrlUpload;

namespace SbaCars.Inventory.Api.Contracts;

public sealed record GerarUrlUploadEvidenciaRequest
{
    public string NomeArquivo { get; init; } = string.Empty;

    public string TipoConteudo { get; init; } = string.Empty;

    public long TamanhoBytes { get; init; }

    public GerarUrlUploadCommand ToCommand(Guid ofertaId) => new()
    {
        OfertaId = ofertaId,
        NomeArquivo = NomeArquivo,
        TipoConteudo = TipoConteudo,
        TamanhoBytes = TamanhoBytes,
    };
}
