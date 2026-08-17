import { useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { Button } from '@sbacars/ui';
import { useDetalheSolicitacao } from '../api/useSolicitacoes';
import { useAprovarSolicitacao } from '../api/useDecisao';
import { BadgeTipoSolicitacao } from '../components/BadgeTipoSolicitacao';
import { IndicadorSla } from '../components/IndicadorSla';
import { ChecklistElegibilidade } from '../components/ChecklistElegibilidade';
import { ModalRejeicao } from '../components/ModalRejeicao';
import { ErrorState } from '@/shared/components/ErrorState';
import { ApiError } from '@/shared/api/problemDetails';
import { formatarDataHora } from '@/shared/formatters/data';
import { formatarPlaca } from '@/shared/formatters/placa';

export function DetalheSolicitacaoPage() {
  const { solicitacaoId } = useParams<{ solicitacaoId: string }>();
  const navigate = useNavigate();

  const { data: sol, isLoading, isError, error, refetch } = useDetalheSolicitacao(solicitacaoId);
  const aprovarMutation = useAprovarSolicitacao();

  const [modalRejeicaoAberto, setModalRejeicaoAberto] = useState(false);
  const [erroDecisao, setErroDecisao] = useState<string | null>(null);

  if (isLoading) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <p className="text-sm text-neutral-600">Carregando detalhes da solicitação…</p>
      </div>
    );
  }

  if (isError || !sol || !solicitacaoId) {
    return (
      <ErrorState
        mensagem={error instanceof Error ? error.message : 'Solicitação não encontrada.'}
        onRetry={() => refetch()}
      />
    );
  }

  const handleAprovar = () => {
    setErroDecisao(null);
    aprovarMutation.mutate(
      { id: solicitacaoId },
      {
        onSuccess: () => {
          refetch();
        },
        onError: (err) => {
          if (err instanceof ApiError) {
            setErroDecisao(err.problem.detail ?? err.problem.title);
          } else {
            setErroDecisao('Erro ao aprovar solicitação.');
          }
        },
      }
    );
  };

  const isPendente = sol.status === 'pendente';
  const podeDecidir = sol.podeDecidir !== false;

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <ModalRejeicao
        isOpen={modalRejeicaoAberto}
        solicitacaoId={solicitacaoId}
        onClose={() => setModalRejeicaoAberto(false)}
        onSuccess={() => refetch()}
      />

      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-xs text-neutral-500 font-medium">
        <Link to="/validacao" className="hover:text-neutral-900 transition-colors">
          Fila de Validação
        </Link>
        <span>/</span>
        <span className="text-neutral-900 font-semibold">Solicitação #{solicitacaoId.slice(0, 8)}</span>
      </nav>

      {/* Cabeçalho */}
      <div className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <BadgeTipoSolicitacao tipo={sol.tipo} />
            <h1 className="text-xl font-bold text-neutral-900">
              {sol.descricaoVeiculo || 'Veículo sob validação'}
            </h1>
            <span className="text-xs font-mono font-bold text-neutral-700">
              {formatarPlaca(sol.placa)}
            </span>
          </div>

          <Link
            to={`/estoque/${sol.ofertaId}`}
            className="text-xs font-semibold text-primary hover:underline self-start sm:self-auto"
          >
            Ver oferta no estoque →
          </Link>
        </div>

        <div className="flex flex-wrap items-center gap-4 text-xs text-neutral-600 border-t border-border pt-3">
          <span>
            Solicitado por: <strong>{sol.abertaPor.nome}</strong> em {formatarDataHora(sol.abertaEm)}
          </span>
          <span>•</span>
          <div className="flex items-center gap-1.5">
            <span>Tempo em espera:</span>
            <IndicadorSla abertaEm={sol.abertaEm} foraDoSla={sol.foraDoSla} />
          </div>
          <span>•</span>
          <span>
            Status:{' '}
            <strong
              className={[
                'uppercase',
                sol.status === 'pendente'
                  ? 'text-amber-800'
                  : sol.status === 'aprovada'
                  ? 'text-emerald-800'
                  : 'text-red-800',
              ].join(' ')}
            >
              {sol.status}
            </strong>
          </span>
        </div>
      </div>

      {erroDecisao && (
        <div className="rounded-xl border border-danger/30 bg-danger/10 p-4 text-sm text-danger font-medium">
          {erroDecisao}
        </div>
      )}

      {/* Aviso de Segregação de Funções (DUX-08) */}
      {isPendente && !podeDecidir && (
        <div className="rounded-xl border border-amber-300 bg-amber-50 p-4 text-xs text-amber-900 flex items-start gap-2.5 shadow-xs">
          <span className="text-base">ℹ️</span>
          <div>
            <strong className="font-bold">Segregação de funções (DUX-08):</strong>
            <p className="mt-0.5">
              Você é o autor desta solicitação e não pode aprová-la ou rejeitá-la. A decisão deve ser
              tomada por outro operador responsável.
            </p>
          </div>
        </div>
      )}

      {/* Comparação Antes vs Depois */}
      <section className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-4">
        <h2 className="text-base font-bold text-neutral-900 border-b border-border pb-3">
          Alteração Proposta
        </h2>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div className="rounded-lg border border-border bg-neutral-50 p-4">
            <span className="text-xs font-bold uppercase tracking-wider text-muted">Estado Vigente</span>
            <p className="text-base font-semibold text-neutral-800 mt-1">{sol.valorVigente || '—'}</p>
          </div>

          <div className="rounded-lg border border-primary/40 bg-primary/5 p-4">
            <span className="text-xs font-bold uppercase tracking-wider text-primary font-semibold">
              Proposta Submetida
            </span>
            <p className="text-base font-bold text-neutral-900 mt-1">{sol.valorProposto || '—'}</p>
          </div>
        </div>

        {/* Justificativa */}
        <div className="rounded-lg border border-border/80 bg-background p-4 space-y-1">
          <span className="text-xs font-bold uppercase tracking-wider text-neutral-700">
            Justificativa do Solicitante
          </span>
          <p className="text-sm text-neutral-800 italic">
            &quot;{sol.justificativa}&quot;
          </p>
        </div>

        {/* Impacto ao Aprovar */}
        {sol.impactoAoAprovar && (
          <div className="rounded-lg border border-blue-200 bg-blue-50 p-4 text-xs text-blue-900">
            <strong>Impacto da aprovação:</strong> {sol.impactoAoAprovar}
          </div>
        )}

        {/* Checklist Proposto (para solicitações de elegibilidade) */}
        {sol.elegibilidadeProposta && (
          <div className="mt-4">
            <ChecklistElegibilidade checklist={sol.elegibilidadeProposta} />
          </div>
        )}
      </section>

      {/* Box de Decisão Registrada (quando aprovada ou rejeitada) */}
      {!isPendente && sol.decisao && (
        <section className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-3">
          <h2 className="text-base font-bold text-neutral-900 border-b border-border pb-3">
            Decisão Registrada
          </h2>
          <div className="text-xs text-neutral-700 space-y-1.5">
            <p>
              Resultado:{' '}
              <strong className="uppercase font-bold text-neutral-900">{sol.decisao.status}</strong>
            </p>
            <p>
              Decidido por: <strong>{sol.decisao.decididaPor.nome}</strong> em{' '}
              {formatarDataHora(sol.decisao.decididaEm)}
            </p>
            {sol.decisao.justificativa && (
              <div className="mt-2 rounded-lg bg-neutral-100 p-3 text-neutral-800">
                <span className="font-semibold block mb-0.5">Motivo registrado:</span>
                <p className="italic">&quot;{sol.decisao.justificativa}&quot;</p>
              </div>
            )}
          </div>
        </section>
      )}

      {/* Botões de Ação para Decisor */}
      {isPendente && (
        <div className="flex items-center justify-end gap-3 pt-4 border-t border-border">
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate('/validacao')}
          >
            Voltar para fila
          </Button>

          <Button
            type="button"
            variant="danger"
            disabled={!podeDecidir || aprovarMutation.isPending}
            onClick={() => setModalRejeicaoAberto(true)}
          >
            Rejeitar solicitação
          </Button>

          <Button
            type="button"
            variant="primary"
            disabled={!podeDecidir || aprovarMutation.isPending}
            onClick={handleAprovar}
          >
            {aprovarMutation.isPending ? 'Aprovando…' : 'Aprovar solicitação'}
          </Button>
        </div>
      )}
    </div>
  );
}
