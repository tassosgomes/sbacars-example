using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Solicitacoes;
using SbaCars.Inventory.Application.Solicitacoes.ListarFilaValidacao;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Infrastructure.Solicitacoes;

public sealed class SolicitacaoRepository(
    InventoryDbContext context,
    CalculadoraDiasUteis calculadora) : Repository<Solicitacao>(context), ISolicitacaoRepository, ISolicitacaoReadRepository
{
    private InventoryDbContext InventoryContext => (InventoryDbContext)Context;

    public Task<Solicitacao?> ObterAsync(
        Guid solicitacaoId,
        CancellationToken cancellationToken = default) =>
        InventoryContext.Solicitacoes
            .AsTracking()
            .SingleOrDefaultAsync(
                solicitacao => solicitacao.Id == solicitacaoId,
                cancellationToken);

    public void Adicionar(Solicitacao solicitacao) => Add(solicitacao);

    public Task<bool> ExistePendenteAsync(
        Guid ofertaId,
        TipoSolicitacao tipo,
        CancellationToken cancellationToken = default) =>
        InventoryContext.Solicitacoes
            .AsNoTracking()
            .AnyAsync(
                solicitacao => solicitacao.OfertaId == ofertaId &&
                    solicitacao.Tipo == tipo &&
                    solicitacao.Status == StatusSolicitacao.Pendente,
                cancellationToken);

    public async Task<PagedResult<SolicitacaoResumoResponse>> ListarAsync(
        ListarFilaValidacaoQuery query,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        var status = query.Status is null
            ? StatusSolicitacao.Pendente
            : StatusSolicitacaoExtensions.Parse(query.Status);
        var tipos = query.Tipo
            .Select(TipoSolicitacaoExtensions.Parse)
            .ToArray();

        var solicitacoes =
            from solicitacao in InventoryContext.Solicitacoes.AsNoTracking()
            join oferta in InventoryContext.Ofertas.AsNoTracking()
                on solicitacao.OfertaId equals oferta.Id
            where solicitacao.Status == status &&
                (tipos.Length == 0 || tipos.Contains(solicitacao.Tipo))
            select new
            {
                SolicitacaoId = solicitacao.Id,
                OfertaId = solicitacao.OfertaId,
                Placa = oferta.Veiculo.Placa,
                Marca = oferta.Veiculo.Marca,
                Modelo = oferta.Veiculo.Modelo,
                Versao = oferta.Veiculo.Versao,
                Tipo = solicitacao.Tipo,
                Status = solicitacao.Status,
                NovoPrecoCentavos = solicitacao.NovoPrecoCentavos,
                AbertaEm = solicitacao.AbertaEm,
                AbertaPorUsuarioId = solicitacao.AbertaPor.UsuarioId,
                AbertaPorNome = solicitacao.AbertaPor.Nome,
                PrecoVigenteCentavos = oferta.PrecoOficial == null
                    ? (long?)null
                    : oferta.PrecoOficial.ValorCentavos,
                SituacaoOferta = oferta.Situacao,
                DisponibilidadeOferta = oferta.Disponibilidade.Estado,
            };

        solicitacoes = query.OrdenarPor switch
        {
            "abertaEm:desc" => solicitacoes.OrderByDescending(item => item.AbertaEm),
            "decididaEm:desc" => solicitacoes.OrderByDescending(item => item.AbertaEm),
            _ => solicitacoes.OrderBy(item => item.AbertaEm),
        };

        var totalCount = await solicitacoes
            .LongCountAsync(cancellationToken)
            .ConfigureAwait(false);
        var rows = await solicitacoes
            .Skip(query.Paginacao.Skip)
            .Take(query.Paginacao.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var items = rows
            .Select(row => new SolicitacaoFilaProjection(
                row.SolicitacaoId,
                row.OfertaId,
                row.Placa,
                row.Marca,
                row.Modelo,
                row.Versao,
                row.Tipo,
                row.Status,
                row.NovoPrecoCentavos,
                row.AbertaEm,
                row.AbertaPorUsuarioId,
                row.AbertaPorNome,
                row.PrecoVigenteCentavos,
                row.SituacaoOferta,
                row.DisponibilidadeOferta))
            .Select(item => SolicitacaoResponseMapper.ToResumo(item, calculadora, agora))
            .ToArray();

        return new PagedResult<SolicitacaoResumoResponse>(
            items,
            query.Paginacao.Page,
            query.Paginacao.PageSize,
            totalCount);
    }

    public async Task<ContagemPendentesResponse> ContarPendentesAsync(
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        var pendentes = await InventoryContext.Solicitacoes
            .AsNoTracking()
            .Where(solicitacao => solicitacao.Status == StatusSolicitacao.Pendente)
            .Select(solicitacao => new { solicitacao.Tipo, solicitacao.AbertaEm })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var porTipo = Enum.GetValues<TipoSolicitacao>()
            .ToDictionary(
                tipo => tipo.ToContractValue(),
                tipo => pendentes.Count(item => item.Tipo == tipo),
                StringComparer.Ordinal);
        var foraDoSla = pendentes.Count(item => calculadora.ForaDoSla(item.AbertaEm, agora));
        var response = new ContagemPendentesResponse(pendentes.Count, porTipo, foraDoSla);
        InventoryMeters.SetPendingSnapshot(porTipo, foraDoSla);
        return response;
    }

}
