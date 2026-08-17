using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Application.Integracao.ListarOfertasElegiveis;
using SbaCars.Inventory.Application.Ofertas.ListarOfertas;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Infrastructure.Projecoes;

namespace SbaCars.Inventory.Infrastructure.Ofertas;

public sealed class OfertaRepository(
    InventoryDbContext context,
    IEvidenciaRepository evidenciaRepository)
    : Repository<Oferta>(context),
        IOfertaRepository,
        IOfertaReadRepository,
        IOfertaElegivelReadRepository
{
    private InventoryDbContext InventoryContext => (InventoryDbContext)Context;

    public void Adicionar(Oferta oferta) => Add(oferta);

    public void Remover(Oferta oferta) => Remove(oferta);

    public Task<Oferta?> ObterAsync(Guid ofertaId, CancellationToken cancellationToken = default) =>
        InventoryContext.Ofertas
            .AsTracking()
            .SingleOrDefaultAsync(oferta => oferta.Id == ofertaId, cancellationToken);

    public Task<bool> ExistePlacaAtivaAsync(
        string placa,
        Guid? ignorarOfertaId = null,
        CancellationToken cancellationToken = default) =>
        InventoryContext.Ofertas
            .AsNoTracking()
            .AnyAsync(oferta => oferta.Veiculo.Placa == placa &&
                                oferta.Situacao != SituacaoOferta.Retirada &&
                                (ignorarOfertaId == null || oferta.Id != ignorarOfertaId),
                cancellationToken);

    public async Task<OfertaDetalheResponse?> ObterDetalheAsync(
        Guid ofertaId,
        CancellationToken cancellationToken)
    {
        var oferta = await InventoryContext.Ofertas
            .AsNoTracking()
            .SingleOrDefaultAsync(oferta => oferta.Id == ofertaId, cancellationToken)
            .ConfigureAwait(false);

        return oferta is null
            ? null
            : await OfertaDetalheAssembler
                .BuildAsync(oferta, evidenciaRepository, cancellationToken)
                .ConfigureAwait(false);
    }

    public async Task<PagedResult<OfertaResumoResponse>> ListarAsync(
        ListarOfertasQuery query,
        CancellationToken cancellationToken)
    {
        var ofertas = InventoryContext.Ofertas.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Busca))
        {
            var busca = query.Busca.Trim().ToUpperInvariant();
            ofertas = ofertas.Where(oferta =>
                (oferta.Veiculo.Placa != null && oferta.Veiculo.Placa.ToUpper().Contains(busca)) ||
                (oferta.Veiculo.Marca != null && oferta.Veiculo.Marca.ToUpper().Contains(busca)) ||
                (oferta.Veiculo.Modelo != null && oferta.Veiculo.Modelo.ToUpper().Contains(busca)));
        }

        var situacoes = query.Situacao
            .Select(ParseSituacao)
            .ToArray();
        if (situacoes.Length > 0)
        {
            ofertas = ofertas.Where(oferta => situacoes.Contains(oferta.Situacao));
        }

        var disponibilidades = query.Disponibilidade
            .Select(ParseDisponibilidade)
            .ToArray();
        if (disponibilidades.Length > 0)
        {
            ofertas = ofertas.Where(oferta => disponibilidades.Contains(oferta.Disponibilidade.Estado));
        }

        if (!string.IsNullOrWhiteSpace(query.Uf))
        {
            var uf = query.Uf.Trim().ToUpperInvariant();
            ofertas = ofertas.Where(oferta => oferta.Veiculo.Localizacao.Uf == uf);
        }

        ofertas = query.OrdenarPor switch
        {
            "atualizadoEm:asc" => ofertas.OrderBy(oferta => oferta.AtualizadoEm),
            "precoOficialCentavos:desc" => ofertas.OrderByDescending(oferta => oferta.PrecoOficial!.ValorCentavos),
            "precoOficialCentavos:asc" => ofertas.OrderBy(oferta => oferta.PrecoOficial!.ValorCentavos),
            "veiculo:asc" => ofertas.OrderBy(oferta => oferta.Veiculo.Marca).ThenBy(oferta => oferta.Veiculo.Modelo),
            _ => ofertas.OrderByDescending(oferta => oferta.AtualizadoEm),
        };

        var totalCount = await ofertas.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await ofertas
            .Skip(query.Paginacao.Skip)
            .Take(query.Paginacao.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<OfertaResumoResponse>(
            items.Select(OfertaResponseMapper.ToResumo).ToArray(),
            query.Paginacao.Page,
            query.Paginacao.PageSize,
            totalCount);
    }

    public async Task<PagedResult<OfertaElegivelResponse>> ListarAsync(
        ListarOfertasElegiveisQuery query,
        CancellationToken cancellationToken)
    {
        var ofertas = InventoryContext.Ofertas
            .AsNoTracking()
            .Where(oferta => oferta.Situacao == SituacaoOferta.Elegivel &&
                             oferta.PrecoOficial != null);

        if (query.AtualizadoApos is { } atualizadoApos)
        {
            var atualizadoAposUtc = atualizadoApos.ToUniversalTime();
            ofertas = ofertas.Where(oferta => oferta.AtualizadoEm > atualizadoAposUtc);
        }

        var totalCount = await ofertas
            .LongCountAsync(cancellationToken)
            .ConfigureAwait(false);

        var rows = await ofertas
            .OrderByDescending(oferta => oferta.AtualizadoEm)
            .ThenBy(oferta => oferta.Id)
            .Skip(query.Paginacao.Skip)
            .Take(query.Paginacao.PageSize)
            .Select(oferta => new OfertaElegivelProjection
            {
                OfertaId = oferta.Id,
                Placa = oferta.Veiculo.Placa,
                Chassi = oferta.Veiculo.Chassi,
                TipoVeiculo = oferta.Veiculo.TipoVeiculo,
                Marca = oferta.Veiculo.Marca,
                Modelo = oferta.Veiculo.Modelo,
                Versao = oferta.Veiculo.Versao,
                AnoFabricacao = oferta.Veiculo.AnoFabricacao,
                AnoModelo = oferta.Veiculo.AnoModelo,
                Quilometragem = oferta.Veiculo.Quilometragem,
                Cor = oferta.Veiculo.Cor,
                Combustivel = oferta.Veiculo.Combustivel,
                Cambio = oferta.Veiculo.Cambio,
                Cep = oferta.Veiculo.Localizacao.Cep,
                Cidade = oferta.Veiculo.Localizacao.Cidade,
                Uf = oferta.Veiculo.Localizacao.Uf,
                OrigemTipo = oferta.Fatos.Origem.Tipo,
                OrigemIndisponivel = oferta.Fatos.Origem.Indisponivel,
                OrigemDescricao = oferta.Fatos.Origem.Descricao,
                OrigemFonte = oferta.Fatos.Origem.Fonte,
                OrigemLimitacao = oferta.Fatos.Origem.LimitacaoDeclarada,
                CondicaoTipo = oferta.Fatos.Condicao.Tipo,
                CondicaoIndisponivel = oferta.Fatos.Condicao.Indisponivel,
                CondicaoDescricao = oferta.Fatos.Condicao.Descricao,
                CondicaoFonte = oferta.Fatos.Condicao.Fonte,
                CondicaoLimitacao = oferta.Fatos.Condicao.LimitacaoDeclarada,
                HistoricoTipo = oferta.Fatos.Historico.Tipo,
                HistoricoIndisponivel = oferta.Fatos.Historico.Indisponivel,
                HistoricoDescricao = oferta.Fatos.Historico.Descricao,
                HistoricoFonte = oferta.Fatos.Historico.Fonte,
                HistoricoLimitacao = oferta.Fatos.Historico.LimitacaoDeclarada,
                PrecoValorCentavos = oferta.PrecoOficial!.ValorCentavos,
                PrecoMoeda = oferta.PrecoOficial.Moeda,
                Disponibilidade = oferta.Disponibilidade.Estado,
                AtualizadoEm = oferta.AtualizadoEm,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(OfertaElegivelProjectionMapper.ToResponse)
            .ToArray();

        return new PagedResult<OfertaElegivelResponse>(
            items,
            query.Paginacao.Page,
            query.Paginacao.PageSize,
            totalCount);
    }

    private static SituacaoOferta ParseSituacao(string value) => value switch
    {
        "em-preparacao" => SituacaoOferta.EmPreparacao,
        "elegivel" => SituacaoOferta.Elegivel,
        "suspensa" => SituacaoOferta.Suspensa,
        "retirada" => SituacaoOferta.Retirada,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static EstadoDisponibilidade ParseDisponibilidade(string value) => value switch
    {
        "disponivel" => EstadoDisponibilidade.Disponivel,
        "reservado" => EstadoDisponibilidade.Reservado,
        "vendido" => EstadoDisponibilidade.Vendido,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
