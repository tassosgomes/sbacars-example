import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@sbacars/ui';
import { useListarOfertas } from '../api/useOfertas';
import { BadgeSituacao } from '../components/BadgeSituacao';
import { BadgeDisponibilidade } from '../components/BadgeDisponibilidade';
import { BadgeTipoSolicitacao } from '../components/BadgeTipoSolicitacao';
import { DataTable, type Column } from '@/shared/components/DataTable';
import { ErrorState } from '@/shared/components/ErrorState';
import { useFiltrosNaUrl } from '@/shared/hooks/useFiltrosNaUrl';
import { centavosParaBrl } from '@/shared/formatters/moeda';
import { formatarData } from '@/shared/formatters/data';
import { formatarPlaca } from '@/shared/formatters/placa';
import type { OfertaResumo, SituacaoOferta, EstadoDisponibilidade } from '@/shared/api/types';

const situacoesChips: { label: string; valor?: SituacaoOferta }[] = [
  { label: 'Todas' },
  { label: 'Em preparação', valor: 'em-preparacao' },
  { label: 'Elegível', valor: 'elegivel' },
  { label: 'Suspensa', valor: 'suspensa' },
  { label: 'Retirada', valor: 'retirada' },
];

export function ListaEstoquePage() {
  const navigate = useNavigate();
  const { filtros, setFiltro, limparFiltros } = useFiltrosNaUrl<{
    page: number;
    pageSize: number;
    busca: string;
    situacao?: SituacaoOferta;
    disponibilidade?: EstadoDisponibilidade;
    uf?: string;
  }>({
    page: 1,
    pageSize: 20,
    busca: '',
  });

  const [buscaLocal, setBuscaLocal] = useState(filtros.busca ?? '');

  // Debounce de busca de 300ms
  useEffect(() => {
    const timer = setTimeout(() => {
      if (buscaLocal !== (filtros.busca ?? '')) {
        setFiltro('busca', buscaLocal || undefined);
      }
    }, 300);
    return () => clearTimeout(timer);
  }, [buscaLocal, filtros.busca, setFiltro]);

  const { data, isLoading, isError, error, refetch } = useListarOfertas({
    page: filtros.page,
    pageSize: filtros.pageSize,
    busca: filtros.busca ? filtros.busca : undefined,
    situacao: filtros.situacao ? [filtros.situacao] : undefined,
    disponibilidade: filtros.disponibilidade ? [filtros.disponibilidade] : undefined,
    uf: filtros.uf || undefined,
  });

  const columns: Column<OfertaResumo>[] = [
    {
      key: 'veiculo',
      header: 'Veículo',
      render: (item) => (
        <div className="flex flex-col min-w-0">
          <span className="font-semibold text-neutral-900 truncate">
            {item.descricaoVeiculo || 'Veículo sem identificação'}
          </span>
          <span className="text-xs text-neutral-500 font-mono">
            {formatarPlaca(item.placa)}
          </span>
        </div>
      ),
    },
    {
      key: 'ano',
      header: 'Ano',
      render: (item) => (
        <span className="text-neutral-700">
          {item.anoFabricacao ? `${item.anoFabricacao}${item.anoModelo ? `/${item.anoModelo}` : ''}` : '—'}
        </span>
      ),
    },
    {
      key: 'quilometragem',
      header: 'KM',
      render: (item) => (
        <span className="font-mono text-neutral-700">
          {item.quilometragem !== null && item.quilometragem !== undefined
            ? `${item.quilometragem.toLocaleString('pt-BR')} km`
            : '—'}
        </span>
      ),
    },
    {
      key: 'localizacao',
      header: 'Localização',
      render: (item) => (
        <span className="text-neutral-700">
          {item.localizacao?.cidade && item.localizacao?.uf
            ? `${item.localizacao.cidade}/${item.localizacao.uf}`
            : '—'}
        </span>
      ),
    },
    {
      key: 'preco',
      header: 'Preço Oficial',
      render: (item) => (
        <span className="font-semibold font-mono text-neutral-900">
          {centavosParaBrl(item.precoOficialCentavos)}
        </span>
      ),
    },
    {
      key: 'situacao',
      header: 'Situação',
      render: (item) => <BadgeSituacao situacao={item.situacao} />,
    },
    {
      key: 'disponibilidade',
      header: 'Disponibilidade',
      render: (item) => <BadgeDisponibilidade disponibilidade={item.disponibilidade} />,
    },
    {
      key: 'pendencias',
      header: 'Pendências',
      render: (item) => {
        if (!item.pendencias || item.pendencias.length === 0) {
          return <span className="text-neutral-400 text-xs">—</span>;
        }
        return (
          <div className="flex flex-wrap gap-1">
            {item.pendencias.map((tipo) => (
              <BadgeTipoSolicitacao key={tipo} tipo={tipo} />
            ))}
          </div>
        );
      },
    },
    {
      key: 'atualizadoEm',
      header: 'Atualizado em',
      render: (item) => (
        <span className="text-xs text-neutral-500 whitespace-nowrap">
          {formatarData(item.atualizadoEm)}
        </span>
      ),
    },
  ];

  return (
    <div className="flex flex-col gap-6">
      {/* Cabeçalho */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-neutral-900 tracking-tight">Estoque curado</h1>
          <p className="text-sm text-neutral-600 mt-0.5">
            {data ? `${data.totalCount} veículo(s) cadastrado(s)` : 'Carregando estoque…'}
          </p>
        </div>
        <Button
          type="button"
          variant="primary"
          onClick={() => navigate('/estoque/novo')}
          className="self-start sm:self-auto"
        >
          Cadastrar veículo
        </Button>
      </div>

      {/* Barra de Filtros */}
      <div className="flex flex-wrap items-center gap-3 rounded-xl border border-border bg-surface p-4 shadow-xs">
        <div className="relative min-w-[260px] flex-1">
          <input
            type="text"
            value={buscaLocal}
            onChange={(e) => setBuscaLocal(e.target.value)}
            placeholder="Buscar por placa, marca ou modelo…"
            className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 placeholder:text-neutral-500 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>

        {/* Chips de Situação */}
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 sm:pb-0">
          {situacoesChips.map((chip) => {
            const isSelected = chip.valor === undefined ? !filtros.situacao : filtros.situacao === chip.valor;
            return (
              <button
                key={chip.label}
                type="button"
                onClick={() => setFiltro('situacao', chip.valor)}
                className={[
                  'rounded-full px-3 py-1 text-xs font-semibold whitespace-nowrap transition-colors',
                  isSelected
                    ? 'bg-[#2E2E3A] text-white shadow-xs'
                    : 'border border-border bg-surface text-neutral-700 hover:bg-neutral-100',
                ].join(' ')}
              >
                {chip.label}
              </button>
            );
          })}
        </div>

        {/* Selects de Disponibilidade e UF */}
        <div className="flex items-center gap-2 ml-auto">
          <select
            value={filtros.disponibilidade ?? ''}
            onChange={(e) => setFiltro('disponibilidade', (e.target.value as EstadoDisponibilidade) || undefined)}
            className="rounded-lg border border-border bg-background px-3 py-2 text-xs font-medium text-neutral-800 focus:border-primary focus:outline-none"
          >
            <option value="">Disponibilidade (Todas)</option>
            <option value="disponivel">Disponível</option>
            <option value="reservado">Reservado</option>
            <option value="vendido">Vendido</option>
          </select>

          <select
            value={filtros.uf ?? ''}
            onChange={(e) => setFiltro('uf', e.target.value || undefined)}
            className="rounded-lg border border-border bg-background px-3 py-2 text-xs font-medium text-neutral-800 focus:border-primary focus:outline-none"
          >
            <option value="">UF (Todas)</option>
            <option value="SP">SP</option>
            <option value="RJ">RJ</option>
            <option value="MG">MG</option>
            <option value="PR">PR</option>
            <option value="SC">SC</option>
            <option value="RS">RS</option>
            <option value="BA">BA</option>
          </select>

          {(filtros.busca || filtros.situacao || filtros.disponibilidade || filtros.uf) && (
            <button
              type="button"
              onClick={limparFiltros}
              className="text-xs font-semibold text-neutral-600 hover:text-neutral-900 transition-colors px-2 py-1"
            >
              Limpar
            </button>
          )}
        </div>
      </div>

      {/* Tabela de Ofertas */}
      {isError ? (
        <ErrorState
          mensagem={error instanceof Error ? error.message : undefined}
          onRetry={() => refetch()}
        />
      ) : (
        <div className="rounded-xl border border-border bg-surface shadow-xs overflow-hidden">
          <DataTable
            columns={columns}
            data={data?.items}
            keyExtractor={(item) => item.ofertaId}
            onRowClick={(item) => navigate(`/estoque/${item.ofertaId}`)}
            isLoading={isLoading}
            emptyMessage={
              <div className="flex flex-col items-center py-8">
                <p className="text-neutral-600 font-medium">Nenhum veículo encontrado no estoque.</p>
                {(filtros.busca || filtros.situacao || filtros.disponibilidade || filtros.uf) ? (
                  <Button type="button" variant="ghost" size="sm" onClick={limparFiltros} className="mt-2">
                    Limpar filtros aplicados
                  </Button>
                ) : (
                  <Button
                    type="button"
                    variant="primary"
                    size="sm"
                    onClick={() => navigate('/estoque/novo')}
                    className="mt-3"
                  >
                    Cadastrar primeiro veículo
                  </Button>
                )}
              </div>
            }
            caption="Tabela de ofertas de veículos do estoque curado"
          />

          {/* Paginação */}
          {data && data.totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-border px-6 py-4 bg-surface text-xs text-neutral-600">
              <span>
                Página <strong>{data.page}</strong> de <strong>{data.totalPages}</strong> ({data.totalCount} itens)
              </span>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={!data.hasPreviousPage}
                  onClick={() => setFiltro('page', data.page - 1)}
                >
                  Anterior
                </Button>
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={!data.hasNextPage}
                  onClick={() => setFiltro('page', data.page + 1)}
                >
                  Próxima
                </Button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
