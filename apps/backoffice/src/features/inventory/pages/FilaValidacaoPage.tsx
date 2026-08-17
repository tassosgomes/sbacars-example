import { useNavigate } from 'react-router-dom';
import { Button } from '@sbacars/ui';
import { useFilaValidacao, useContagemPendentes } from '../api/useSolicitacoes';
import { BadgeTipoSolicitacao } from '../components/BadgeTipoSolicitacao';
import { IndicadorSla } from '../components/IndicadorSla';
import { DataTable, type Column } from '@/shared/components/DataTable';
import { ErrorState } from '@/shared/components/ErrorState';
import { useFiltrosNaUrl } from '@/shared/hooks/useFiltrosNaUrl';
import { formatarDataHora } from '@/shared/formatters/data';
import { formatarPlaca } from '@/shared/formatters/placa';
import type { SolicitacaoResumo, StatusSolicitacao, TipoSolicitacao, ListarSolicitacoesParams } from '@/shared/api/types';

export function FilaValidacaoPage() {
  const navigate = useNavigate();
  const { filtros, setFiltro, limparFiltros } = useFiltrosNaUrl<{
    page: number;
    pageSize: number;
    status?: StatusSolicitacao;
    tipo?: TipoSolicitacao;
    ordenarPor?: NonNullable<ListarSolicitacoesParams>['ordenarPor'];
  }>({
    page: 1,
    pageSize: 20,
    status: 'pendente',
  });

  const { data: contagem } = useContagemPendentes();
  const { data, isLoading, isError, error, refetch } = useFilaValidacao({
    page: filtros.page,
    pageSize: filtros.pageSize,
    status: filtros.status || 'pendente',
    tipo: filtros.tipo ? [filtros.tipo] : undefined,
    ordenarPor: filtros.ordenarPor,
  });

  const columns: Column<SolicitacaoResumo>[] = [
    {
      key: 'tipo',
      header: 'Tipo',
      render: (item) => <BadgeTipoSolicitacao tipo={item.tipo} />,
    },
    {
      key: 'veiculo',
      header: 'Veículo',
      render: (item) => (
        <div className="flex flex-col min-w-0">
          <span className="font-semibold text-neutral-900 truncate">
            {item.descricaoVeiculo || 'Veículo'}
          </span>
          <span className="text-xs text-neutral-500 font-mono">{formatarPlaca(item.placa)}</span>
        </div>
      ),
    },
    {
      key: 'alteracao',
      header: 'Alteração Proposta',
      render: (item) => (
        <div className="flex items-center gap-2 text-xs">
          <span className="text-neutral-500 line-through">{item.valorVigente || '—'}</span>
          <span className="text-neutral-400">→</span>
          <span className="font-semibold text-neutral-900">{item.valorProposto || '—'}</span>
        </div>
      ),
    },
    {
      key: 'solicitante',
      header: 'Solicitante',
      render: (item) => (
        <div className="flex flex-col text-xs">
          <span className="font-medium text-neutral-800">{item.abertaPor.nome}</span>
          <span className="text-neutral-500">{formatarDataHora(item.abertaEm)}</span>
        </div>
      ),
    },
    {
      key: 'sla',
      header: 'Tempo / SLA',
      render: (item) => (
        <IndicadorSla abertaEm={item.abertaEm} foraDoSla={item.foraDoSla} />
      ),
    },
    {
      key: 'status',
      header: 'Status',
      render: (item) => {
        const config = {
          pendente: 'bg-amber-100 text-amber-900 border-amber-300',
          aprovada: 'bg-emerald-100 text-emerald-900 border-emerald-300',
          rejeitada: 'bg-red-100 text-red-900 border-red-300',
        }[item.status] ?? 'bg-neutral-100 text-neutral-800';

        return (
          <span
            className={[
              'inline-flex items-center rounded border px-2 py-0.5 text-xs font-bold uppercase tracking-wider',
              config,
            ].join(' ')}
          >
            {item.status}
          </span>
        );
      },
    },
  ];

  return (
    <div className="flex flex-col gap-6">
      {/* Cabeçalho */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-neutral-900 tracking-tight">
            Fila de Validação
          </h1>
          <p className="text-sm text-neutral-600 mt-0.5">
            Decisões sob responsabilidade da operação com meta de SLA de 1 dia útil.
          </p>
        </div>
      </div>

      {/* Alerta de SLA se houver itens fora do prazo */}
      {contagem && contagem.foraDoSla > 0 && (
        <div className="rounded-xl border border-red-300 bg-red-50 p-4 text-red-900 flex items-center gap-3 shadow-xs">
          <span className="text-xl">⚠️</span>
          <div>
            <p className="text-sm font-bold">Atenção ao SLA Operacional</p>
            <p className="text-xs text-red-800">
              Existe(m) <strong>{contagem.foraDoSla}</strong> solicitação(ões) pendente(s) há mais de 1 dia útil aguardando decisão.
            </p>
          </div>
        </div>
      )}

      {/* Abas e Filtros */}
      <div className="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-border bg-surface p-4 shadow-xs">
        {/* Status Tabs */}
        <div className="flex items-center gap-1 bg-neutral-100 p-1 rounded-lg">
          {(['pendente', 'aprovada', 'rejeitada'] as StatusSolicitacao[]).map((st) => {
            const isSelected = (filtros.status || 'pendente') === st;
            return (
              <button
                key={st}
                type="button"
                onClick={() => setFiltro('status', st)}
                className={[
                  'rounded-md px-3.5 py-1.5 text-xs font-semibold uppercase tracking-wider transition-colors',
                  isSelected
                    ? 'bg-surface text-neutral-900 shadow-xs'
                    : 'text-neutral-600 hover:text-neutral-900',
                ].join(' ')}
              >
                {st}
                {st === 'pendente' && contagem?.total ? ` (${contagem.total})` : ''}
              </button>
            );
          })}
        </div>

        {/* Filtro por Tipo */}
        <div className="flex items-center gap-3">
          <select
            value={filtros.tipo ?? ''}
            onChange={(e) => setFiltro('tipo', (e.target.value as TipoSolicitacao) || undefined)}
            className="rounded-lg border border-border bg-background px-3 py-2 text-xs font-medium text-neutral-800 focus:border-primary focus:outline-none"
          >
            <option value="">Tipo de alteração (Todos)</option>
            <option value="elegibilidade">Elegibilidade</option>
            <option value="preco">Preço oficial</option>
            <option value="retirada">Retirada</option>
            <option value="reversao-venda">Reversão de venda</option>
          </select>

          {filtros.tipo && (
            <button
              type="button"
              onClick={limparFiltros}
              className="text-xs font-semibold text-neutral-600 hover:text-neutral-900"
            >
              Limpar
            </button>
          )}
        </div>
      </div>

      {/* Tabela de Solicitações */}
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
            keyExtractor={(item) => item.solicitacaoId}
            onRowClick={(item) => navigate(`/validacao/${item.solicitacaoId}`)}
            isLoading={isLoading}
            emptyMessage="Nenhuma solicitação encontrada nesta fila."
            caption="Tabela de solicitações de validação"
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
