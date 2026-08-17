using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Application.Evidencias;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Evidencias.GerarUrlDownload;

public sealed class GerarUrlDownloadHandler(
    IEvidenciaRepository evidenciaRepository,
    IObjectStorage objectStorage,
    IInventoryStorageSettings storageSettings) : IQueryHandler<GerarUrlDownloadQuery, DownloadEvidenciaResponse>
{
    public async Task<DownloadEvidenciaResponse> HandleAsync(
        GerarUrlDownloadQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var evidencia = await evidenciaRepository
            .ObterAsync(query.EvidenciaId, cancellationToken)
            .ConfigureAwait(false);

        if (evidencia is null)
        {
            throw new EvidenciaNaoEncontradaException(query.EvidenciaId);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var presigned = await objectStorage
            .CreateDownloadUrlAsync(
                storageSettings.BucketName,
                evidencia.ObjectKey,
                cancellationToken)
            .ConfigureAwait(false);

        return new DownloadEvidenciaResponse(
            presigned.Url.ToString(),
            presigned.ExpiresAt,
            evidencia.NomeArquivo);
    }
}
