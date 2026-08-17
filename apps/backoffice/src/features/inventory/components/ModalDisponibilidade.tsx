import { useState } from 'react';
import { Button } from '@sbacars/ui';
import { useAlterarDisponibilidade, useAbrirSolicitacao } from '../api/useMutacoesOferta';
import { BadgeDisponibilidade } from './BadgeDisponibilidade';
import { ApiError } from '@/shared/api/problemDetails';
import type { EstadoDisponibilidade } from '@/shared/api/types';

export interface ModalDisponibilidadeProps {
  isOpen: boolean;
  ofertaId: string;
  estadoAtual: EstadoDisponibilidade;
  transicoesPermitidas?: EstadoDisponibilidade[];
  onClose: () => void;
  onSuccess?: () => void;
}

export function ModalDisponibilidade({
  isOpen,
  ofertaId,
  estadoAtual,
  transicoesPermitidas = [],
  onClose,
  onSuccess,
}: ModalDisponibilidadeProps) {
  const [novoEstado, setNovoEstado] = useState<EstadoDisponibilidade | 'reversao-venda'>(
    transicoesPermitidas[0] ?? 'reservado'
  );
  const [justificativa, setJustificativa] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  const alterarDispMutation = useAlterarDisponibilidade(ofertaId);
  const abrirSolicitacaoMutation = useAbrirSolicitacao(ofertaId);

  if (!isOpen) return null;

  const isReversaoVenda = estadoAtual === 'vendido' || novoEstado === 'reversao-venda';

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErro(null);

    if (isReversaoVenda) {
      if (!justificativa.trim() || justificativa.trim().length < 5) {
        setErro('Para reverter uma venda, a justificativa é obrigatória (mínimo 5 caracteres).');
        return;
      }
      abrirSolicitacaoMutation.mutate(
        {
          tipo: 'reversao-venda',
          justificativa: justificativa.trim(),
        },
        {
          onSuccess: () => {
            onSuccess?.();
            onClose();
          },
          onError: (err) => {
            if (err instanceof ApiError) {
              setErro(err.problem.detail ?? err.problem.title);
            } else {
              setErro('Erro ao solicitar reversão de venda.');
            }
          },
        }
      );
    } else {
      alterarDispMutation.mutate(
        {
          novoEstado: novoEstado as EstadoDisponibilidade,
        },
        {
          onSuccess: () => {
            onSuccess?.();
            onClose();
          },
          onError: (err) => {
            if (err instanceof ApiError) {
              setErro(err.problem.detail ?? err.problem.title);
            } else {
              setErro('Erro ao alterar disponibilidade.');
            }
          },
        }
      );
    }
  };

  const isPending = alterarDispMutation.isPending || abrirSolicitacaoMutation.isPending;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-xs animate-fade-in"
      role="dialog"
      aria-modal="true"
      aria-labelledby="modal-disponibilidade-titulo"
    >
      <div className="w-full max-w-lg rounded-xl border border-border bg-surface p-6 shadow-xl">
        <h2 id="modal-disponibilidade-titulo" className="text-lg font-bold text-neutral-900">
          Alterar estado de disponibilidade
        </h2>
        <div className="mt-2 flex items-center gap-2 text-xs text-neutral-600">
          <span>Estado atual:</span>
          <BadgeDisponibilidade disponibilidade={estadoAtual} />
        </div>

        {erro && (
          <div className="mt-3 rounded-lg border border-danger/30 bg-danger/10 p-3 text-xs text-danger font-medium">
            {erro}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div>
            <label className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-2">
              Selecione o novo estado
            </label>

            <div className="space-y-2">
              {transicoesPermitidas.map((estado) => (
                <label
                  key={estado}
                  className={[
                    'flex items-center gap-3 rounded-lg border p-3 cursor-pointer transition-colors text-sm',
                    novoEstado === estado
                      ? 'border-primary bg-primary/5 font-semibold text-primary'
                      : 'border-border hover:bg-neutral-50 text-neutral-800',
                  ].join(' ')}
                >
                  <input
                    type="radio"
                    name="estadoDisponibilidade"
                    value={estado}
                    checked={novoEstado === estado}
                    onChange={() => setNovoEstado(estado)}
                    className="text-primary focus:ring-primary"
                  />
                  <span>
                    {estado === 'disponivel' && 'Disponível (Liberar para oferta e negociação)'}
                    {estado === 'reservado' && 'Reservado (Comprometido com proposta em andamento)'}
                    {estado === 'vendido' && 'Vendido (Negócio concluído)'}
                  </span>
                </label>
              ))}

              {estadoAtual === 'vendido' && (
                <label
                  className={[
                    'flex items-center gap-3 rounded-lg border p-3 cursor-pointer transition-colors text-sm border-purple-200 bg-purple-50 text-purple-900',
                    novoEstado === 'reversao-venda' ? 'ring-2 ring-purple-600' : '',
                  ].join(' ')}
                >
                  <input
                    type="radio"
                    name="estadoDisponibilidade"
                    value="reversao-venda"
                    checked={novoEstado === 'reversao-venda'}
                    onChange={() => setNovoEstado('reversao-venda')}
                    className="text-purple-600 focus:ring-purple-600"
                  />
                  <div>
                    <span className="font-bold">Solicitar Reversão de Venda</span>
                    <p className="text-xs text-purple-800 mt-0.5">
                      Devolver um veículo vendido para disponível exige aprovação na fila de validação (RF-05).
                    </p>
                  </div>
                </label>
              )}
            </div>
          </div>

          {isReversaoVenda && (
            <div>
              <label htmlFor="justificativaReversao" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Justificativa da Reversão *
              </label>
              <textarea
                id="justificativaReversao"
                rows={3}
                maxLength={500}
                placeholder="Explique o motivo do cancelamento da venda (ex: desistência do comprador, distrato, etc.)"
                value={justificativa}
                onChange={(e) => setJustificativa(e.target.value)}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none"
              />
            </div>
          )}

          <div className="flex justify-end gap-3 border-t border-border pt-4 mt-6">
            <Button
              type="button"
              variant="secondary"
              onClick={onClose}
              disabled={isPending}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              disabled={isPending}
            >
              {isPending ? 'Salvando…' : isReversaoVenda ? 'Enviar solicitação' : 'Confirmar transição'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
