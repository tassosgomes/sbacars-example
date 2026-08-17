using SbaCars.BuildingBlocks.Application.Cqrs;

namespace SbaCars.Inventory.Application.Ofertas.ExcluirOferta;

public sealed record ExcluirOfertaCommand(Guid OfertaId) : ICommand<bool>;
