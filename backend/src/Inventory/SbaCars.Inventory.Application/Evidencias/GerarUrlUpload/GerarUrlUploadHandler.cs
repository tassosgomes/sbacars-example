using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Application.Evidencias;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Evidencias.GerarUrlUpload;

public sealed class GerarUrlUploadHandler(
    IOfertaRepository ofertaRepository,
    IEvidenciaRepository evidenciaRepository,
    IObjectStorage objectStorage,
    IInventoryStorageSettings storageSettings,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : ICommandHandler<GerarUrlUploadCommand, UploadEvidenciaResponse>
{
    public async Task<UploadEvidenciaResponse> HandleAsync(
        GerarUrlUploadCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var oferta = await ofertaRepository
            .ObterAsync(command.OfertaId, cancellationToken)
            .ConfigureAwait(false);

        if (oferta is null)
        {
            throw new OfertaNaoEncontradaException(command.OfertaId);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var agora = clock.UtcNow;
        var autoria = new Autoria(
            currentUser.UserId ?? "system",
            currentUser.DisplayName ?? currentUser.UserId ?? "system",
            agora);

        var evidencia = Evidencia.Criar(
            command.OfertaId,
            command.NomeArquivo,
            command.TipoConteudo,
            command.TamanhoBytes,
            autoria,
            agora);

        evidenciaRepository.Adicionar(evidencia);

        var presigned = await objectStorage
            .CreateUploadUrlAsync(
                storageSettings.BucketName,
                evidencia.ObjectKey,
                evidencia.TipoConteudo,
                cancellationToken)
            .ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new UploadEvidenciaResponse(
            evidencia.Id,
            presigned.Url.ToString(),
            presigned.ExpiresAt,
            presigned.RequiredHeaders);
    }
}
