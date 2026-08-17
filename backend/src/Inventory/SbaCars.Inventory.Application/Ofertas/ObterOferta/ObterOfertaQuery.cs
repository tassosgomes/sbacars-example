using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Ofertas.ObterOferta;

public sealed record ObterOfertaQuery(Guid OfertaId) : IQuery<OfertaDetalheResponse>;
