using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Solicitacoes.ContarPendentes;

public sealed record ContarPendentesQuery : IQuery<ContagemPendentesResponse>;
