import { useState } from 'react';
import { Button } from '@sbacars/ui';
import { useAbrirSolicitacao } from '../api/useMutacoesOferta';
import { ApiError } from '@/shared/api/problemDetails';

export interface ModalSolicitarElegibilidadeProps {
  isOpen: boolean;
  ofertaId: string;
  onClose: () => void;
  onSuccess?: () => void;
}

export function ModalSolicitarElegibilidade({
  isOpen,
  ofertaId,
  onClose,
  onSuccess,
}: ModalSolicitarElegibilidadeProps) {
  const [justificativa, setJustificativa] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  const abrirSolicitacaoMutation = useAbrirSolicitacao(ofertaId);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErro(null);

    if (!justificativa.trim() || justificativa.trim().length < 5) {
      setErro('A justificativa é obrigatória (mínimo 5 caracteres).');
      return;
    }

    abrirSolicitacaoMutation.mutate(
      {
        tipo: 'elegibilidade',
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
            setErro('Erro ao solicitar elegibilidade da oferta.');
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
      aria-labelledby="modal-solicitar-elegibilidade-titulo"
    >
      <div className="w-full max-w-lg rounded-xl border border-border bg-surface p-6 shadow-xl">
        <h2 id="modal-solicitar-elegibilidade-titulo" className="text-lg font-bold text-neutral-900">
          Solicitar Elegibilidade para Catálogo Público
        </h2>
        <p className="mt-1 text-xs text-neutral-600">
          Todos os 6 critérios mínimos foram atendidos. Ao submeter, a solicitação entrará na fila de
          validação com SLA de 1 dia útil para aprovação pelo Responsável.
        </p>

        {erro && (
          <div className="mt-3 rounded-lg border border-danger/30 bg-danger/10 p-3 text-xs text-danger font-medium">
            {erro}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div>
            <label htmlFor="justificativaElegibilidade" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
              Justificativa / Observações da Curadoria *
            </label>
            <textarea
              id="justificativaElegibilidade"
              rows={4}
              maxLength={500}
              placeholder="Ex: Cadastro completo, documentação de origem conferida e laudo anexado."
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
              variant="primary"
              disabled={abrirSolicitacaoMutation.isPending}
            >
              {abrirSolicitacaoMutation.isPending ? 'Enviando…' : 'Enviar para validação'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
