using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Ofertas.AtualizarVeiculo;

public sealed class AtualizarVeiculoHandler(
    IOfertaRepository ofertaRepository,
    IUnitOfWork unitOfWork,
    IEstoqueIntegrationEventPublisher integrationEventPublisher,
    ICurrentUser currentUser,
    IClock clock) : ICommandHandler<AtualizarVeiculoCommand, OfertaDetalheResponse>
{
    public async Task<OfertaDetalheResponse> HandleAsync(
        AtualizarVeiculoCommand command,
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

        var placa = command.PlacaInformada ? NormalizePlate(command.Placa) : null;
        if (placa is not null && await ofertaRepository
                .ExistePlacaAtivaAsync(
                    placa,
                    ignorarOfertaId: oferta.Id,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false))
        {
            throw new PlacaDuplicadaException(placa);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var agora = clock.UtcNow;
        var autoria = new Autoria(
            currentUser.UserId ?? "system",
            currentUser.DisplayName ?? currentUser.UserId ?? "system",
            agora);

        oferta.AtualizarVeiculo(
            command.ToDomainPatch() with { Placa = placa },
            autoria,
            agora,
            command.ConfirmaSuspensao);

        cancellationToken.ThrowIfCancellationRequested();
        await integrationEventPublisher.PublishOfferUpdatedAsync(
            oferta.Id,
            agora,
            cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return OfertaResponseMapper.ToDetalhe(oferta);
    }

    private static string? NormalizePlate(string? placa) =>
        string.IsNullOrWhiteSpace(placa)
            ? null
            : placa.Trim().Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}
