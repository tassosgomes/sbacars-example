using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Solicitacoes.ListarFilaValidacao;

public sealed record ListarFilaValidacaoQuery : IQuery<PagedResult<SolicitacaoResumoResponse>>
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;

    public string? Status { get; init; } = "pendente";

    public IReadOnlyCollection<string> Tipo { get; init; } = [];

    public string OrdenarPor { get; init; } = "abertaEm:asc";

    public PagedRequest Paginacao => new(Page, PageSize);
}
