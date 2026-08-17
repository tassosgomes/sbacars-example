import { useState } from 'react';
import { Button } from '@sbacars/ui';
import { useRejeitarSolicitacao } from '../api/useDecisao';
import { ApiError } from '@/shared/api/problemDetails';

export interface ModalRejeicaoProps {
  isOpen: boolean;
  solicitacaoId: string;
  onClose: () => void;
  onSuccess?: () => void;
}

export function ModalRejeicao({ isOpen, solicitacaoId, onClose, onSuccess }: ModalRejeicaoProps) {
  const [justificativa, setJustificativa] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  const rejeitarMutation = useRejeitarSolicitacao();

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErro(null);

    if (!justificativa.trim() || justificativa.trim().length < 5) {
      setErro('O motivo da rejeição é obrigatório e deve conter no mínimo 5 caracteres.');
      return;
    }

    rejeitarMutation.mutate(
      {
        id: solicitacaoId,
        input: { justificativa: justificativa.trim() },
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
            setErro('Erro ao registrar rejeição da solicitação.');
          }
        },
      }
    );
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-xs animate-fade-in"
      role="dialog"
      aria-modal="true"
      aria-labelledby="modal-rejeicao-titulo"
    >
      <div className="w-full max-w-lg rounded-xl border border-border bg-surface p-6 shadow-xl">
        <h2 id="modal-rejeicao-titulo" className="text-lg font-bold text-danger">
          Rejeitar Solicitação de Validação
        </h2>
        <p className="mt-1 text-xs text-neutral-600">
          O estado vigente da oferta permanecerá intacto. O motivo informado abaixo será devolvido ao
          operador para que as devidas correções sejam providenciadas (RF-02).
        </p>

        {erro && (
          <div className="mt-3 rounded-lg border border-danger/30 bg-danger/10 p-3 text-xs text-danger font-medium">
            {erro}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div>
            <label htmlFor="motivoRejeicao" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
              Motivo da Rejeição *
            </label>
            <textarea
              id="motivoRejeicao"
              rows={4}
              maxLength={500}
              placeholder="Descreva claramente o motivo da rejeição e o que deve ser ajustado antes de nova submissão…"
              value={justificativa}
              onChange={(e) => setJustificativa(e.target.value)}
              className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-danger focus:outline-none focus:ring-1 focus:ring-danger"
              autoFocus
            />
            <span className="text-[11px] text-neutral-500 float-right mt-0.5">
              {justificativa.length}/500 caracteres
            </span>
          </div>

          <div className="flex justify-end gap-3 border-t border-border pt-4 mt-6">
            <Button
              type="button"
              variant="secondary"
              onClick={onClose}
              disabled={rejeitarMutation.isPending}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="danger"
              disabled={rejeitarMutation.isPending}
            >
              {rejeitarMutation.isPending ? 'Rejeitando…' : 'Confirmar rejeição'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
