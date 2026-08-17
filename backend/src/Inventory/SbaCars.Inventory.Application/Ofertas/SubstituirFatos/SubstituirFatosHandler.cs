using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Ofertas.SubstituirFatos;

public sealed class SubstituirFatosHandler(
    IOfertaRepository ofertaRepository,
    IEvidenciaRepository evidenciaRepository,
    IUnitOfWork unitOfWork,
    IEstoqueIntegrationEventPublisher integrationEventPublisher,
    ICurrentUser currentUser,
    IClock clock) : ICommandHandler<SubstituirFatosCommand, OfertaDetalheResponse>
{
    public async Task<OfertaDetalheResponse> HandleAsync(
        SubstituirFatosCommand command,
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

        await ValidarEvidenciasAsync(command, cancellationToken).ConfigureAwait(false);

        var agora = clock.UtcNow;
        var autoria = new Autoria(
            currentUser.UserId ?? "system",
            currentUser.DisplayName ?? currentUser.UserId ?? "system",
            agora);
        var fatos = command.ToDomain(autoria);

        // Oferta.SubstituirFatos evaluates a candidate before replacing the owned value object.
        // Consequently, SuspensaoNaoConfirmadaException leaves the tracked aggregate unchanged.
        oferta.SubstituirFatos(fatos, autoria, agora, command.ConfirmaSuspensao);

        cancellationToken.ThrowIfCancellationRequested();
        await integrationEventPublisher.PublishOfferUpdatedAsync(
            oferta.Id,
            agora,
            cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await OfertaDetalheAssembler
            .BuildAsync(oferta, evidenciaRepository, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ValidarEvidenciasAsync(
        SubstituirFatosCommand command,
        CancellationToken cancellationToken)
    {
        var ids = EvidenciaLookup.ColetarIds(
            command.Origem?.EvidenciaId,
            command.Condicao?.EvidenciaId,
            command.Historico?.EvidenciaId);

        if (ids.Count == 0)
        {
            return;
        }

        var evidencias = await evidenciaRepository
            .ObterVariosAsync(ids, cancellationToken)
            .ConfigureAwait(false);
        var map = evidencias.ToDictionary(evidencia => evidencia.Id);

        foreach (var evidenciaId in ids)
        {
            if (!map.TryGetValue(evidenciaId, out var evidencia) ||
                evidencia.OfertaId != command.OfertaId)
            {
                throw new EvidenciaNaoEncontradaException(evidenciaId);
            }
        }
    }
}
