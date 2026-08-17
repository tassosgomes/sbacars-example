using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Ofertas.AlterarDisponibilidade;

public sealed record AlterarDisponibilidadeCommand : ICommand<OfertaDetalheResponse>
{
    public Guid OfertaId { get; init; }

    public EstadoDisponibilidade NovoEstado { get; init; }

    public string? Observacao { get; init; }
}
