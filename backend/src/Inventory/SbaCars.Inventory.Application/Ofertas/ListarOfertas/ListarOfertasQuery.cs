using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Ofertas.ListarOfertas;

public sealed record ListarOfertasQuery : IQuery<PagedResult<OfertaResumoResponse>>
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;

    public string? Busca { get; init; }

    public IReadOnlyCollection<string> Situacao { get; init; } = [];

    public IReadOnlyCollection<string> Disponibilidade { get; init; } = [];

    public string? Uf { get; init; }

    public string OrdenarPor { get; init; } = "atualizadoEm:desc";

    public PagedRequest Paginacao => new(Page, PageSize);
}
