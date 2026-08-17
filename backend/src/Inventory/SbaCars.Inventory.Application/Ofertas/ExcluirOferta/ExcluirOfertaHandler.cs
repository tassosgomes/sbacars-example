using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Ofertas.ExcluirOferta;

public sealed class ExcluirOfertaHandler(
    IOfertaRepository ofertaRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ExcluirOfertaCommand, bool>
{
    public async Task<bool> HandleAsync(
        ExcluirOfertaCommand command,
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

        oferta.Excluir();
        ofertaRepository.Remover(oferta);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
