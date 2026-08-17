using Microsoft.Extensions.Logging;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Ofertas.AlterarDisponibilidade;

public sealed class AlterarDisponibilidadeCommandHandler(
    IOfertaRepository ofertaRepository,
    IUnitOfWork unitOfWork,
    IEstoqueIntegrationEventPublisher integrationEventPublisher,
    ICurrentUser currentUser,
    IClock clock,
    ILogger<AlterarDisponibilidadeCommandHandler> logger)
    : ICommandHandler<AlterarDisponibilidadeCommand, OfertaDetalheResponse>
{
    public async Task<OfertaDetalheResponse> HandleAsync(
        AlterarDisponibilidadeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        using var activity = InventoryActivitySource.Instance.StartActivity(
            "inventory.oferta.disponibilidade.alterar");
        activity?.SetTag("oferta.id", command.OfertaId);
        activity?.SetTag("disponibilidade.novo_estado", command.NovoEstado.ToContractValue());

        var oferta = await ofertaRepository
            .ObterAsync(command.OfertaId, cancellationToken)
            .ConfigureAwait(false);

        if (oferta is null)
        {
            throw new OfertaNaoEncontradaException(command.OfertaId);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var estadoAnterior = oferta.Disponibilidade.Estado;
        var agora = clock.UtcNow;
        var autoria = new Autoria(
            currentUser.UserId ?? "system",
            currentUser.DisplayName ?? currentUser.UserId ?? "system",
            agora);

        oferta.AlterarDisponibilidade(
            command.NovoEstado,
            command.Observacao,
            autoria,
            agora);

        cancellationToken.ThrowIfCancellationRequested();
        await integrationEventPublisher.PublishAvailabilityChangedAsync(
            oferta.Id,
            oferta.Disponibilidade.Estado.ToContractValue(),
            agora,
            cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Disponibilidade da oferta {OfertaId} alterada de {EstadoAnterior} para {NovoEstado} por {AlteradaPor}.",
            oferta.Id,
            estadoAnterior.ToContractValue(),
            oferta.Disponibilidade.Estado.ToContractValue(),
            autoria.UsuarioId);

        return OfertaResponseMapper.ToDetalhe(oferta);
    }
}
