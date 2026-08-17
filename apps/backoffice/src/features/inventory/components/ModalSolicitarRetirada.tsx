import { useState } from 'react';
import { Button } from '@sbacars/ui';
import { useAbrirSolicitacao } from '../api/useMutacoesOferta';
import { ApiError } from '@/shared/api/problemDetails';

export interface ModalSolicitarRetiradaProps {
  isOpen: boolean;
  ofertaId: string;
  onClose: () => void;
  onSuccess?: () => void;
}

export function ModalSolicitarRetirada({
  isOpen,
  ofertaId,
  onClose,
  onSuccess,
}: ModalSolicitarRetiradaProps) {
  const [justificativa, setJustificativa] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  const abrirSolicitacaoMutation = useAbrirSolicitacao(ofertaId);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErro(null);

    if (!justificativa.trim() || justificativa.trim().length < 5) {
      setErro('A justificativa da retirada é obrigatória (mínimo 5 caracteres).');
      return;
    }

    abrirSolicitacaoMutation.mutate(
      {
        tipo: 'retirada',
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
            setErro('Erro ao solicitar retirada da oferta.');
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
      aria-labelledby="modal-solicitar-retirada-titulo"
    >
      <div className="w-full max-w-lg rounded-xl border border-border bg-surface p-6 shadow-xl">
        <h2 id="modal-solicitar-retirada-titulo" className="text-lg font-bold text-neutral-900">
          Solicitar Retirada da Curadoria
        </h2>
        <p className="mt-1 text-xs text-neutral-600">
          A retirada remove o veículo do catálogo e encerra a responsabilidade operacional desta oferta.
          Exige aprovação na fila de validação (RF-02).
        </p>

        {erro && (
          <div className="mt-3 rounded-lg border border-danger/30 bg-danger/10 p-3 text-xs text-danger font-medium">
            {erro}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div>
            <label htmlFor="justificativaRetirada" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
              Motivo da Retirada *
            </label>
            <textarea
              id="justificativaRetirada"
              rows={4}
              maxLength={500}
              placeholder="Explique o motivo da retirada (ex: devolução ao fornecedor, avaria grave, etc.)"
              value={justificativa}
              onChange={(e) => setJustificativa(e.target.value)}
              className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none"
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
              disabled={abrirSolicitacaoMutation.isPending}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="danger"
              disabled={abrirSolicitacaoMutation.isPending}
            >
              {abrirSolicitacaoMutation.isPending ? 'Enviando…' : 'Solicitar retirada'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
