using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Ofertas.DefinirPrecoInicial;

public sealed record DefinirPrecoInicialCommand : ICommand<OfertaDetalheResponse>
{
    public Guid OfertaId { get; init; }

    public long ValorCentavos { get; init; }
}
