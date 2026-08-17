using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Integracao.ListarOfertasElegiveis;

public sealed record ListarOfertasElegiveisQuery : IQuery<PagedResult<OfertaElegivelResponse>>
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;

    public DateTimeOffset? AtualizadoApos { get; init; }

    public PagedRequest Paginacao => new(Page, PageSize);
}
