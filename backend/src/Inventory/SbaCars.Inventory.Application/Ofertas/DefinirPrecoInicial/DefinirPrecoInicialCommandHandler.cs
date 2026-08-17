using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Ofertas.DefinirPrecoInicial;

public sealed class DefinirPrecoInicialCommandHandler(
    IOfertaRepository ofertaRepository,
    IUnitOfWork unitOfWork,
    IEstoqueIntegrationEventPublisher integrationEventPublisher,
    ICurrentUser currentUser,
    IClock clock) : ICommandHandler<DefinirPrecoInicialCommand, OfertaDetalheResponse>
{
    public async Task<OfertaDetalheResponse> HandleAsync(
        DefinirPrecoInicialCommand command,
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
        oferta.DefinirPrecoInicial(
            command.ValorCentavos,
            currentUser.UserId ?? "system",
            currentUser.DisplayName ?? currentUser.UserId ?? "system",
            agora);

        await integrationEventPublisher.PublishOfferUpdatedAsync(
            oferta.Id,
            agora,
            cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return OfertaResponseMapper.ToDetalhe(oferta);
    }
}
