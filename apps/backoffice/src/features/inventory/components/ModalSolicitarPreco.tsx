import { useState } from 'react';
import { Button } from '@sbacars/ui';
import { centavosParaBrl, brlParaCentavos } from '@/shared/formatters/moeda';
import { useAbrirSolicitacao } from '../api/useMutacoesOferta';
import { ApiError } from '@/shared/api/problemDetails';

export interface ModalSolicitarPrecoProps {
  isOpen: boolean;
  ofertaId: string;
  precoAtualCentavos?: number | null;
  onClose: () => void;
  onSuccess?: () => void;
}

export function ModalSolicitarPreco({
  isOpen,
  ofertaId,
  precoAtualCentavos,
  onClose,
  onSuccess,
}: ModalSolicitarPrecoProps) {
  const [novoValorTexto, setNovoValorTexto] = useState('');
  const [justificativa, setJustificativa] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  const abrirSolicitacaoMutation = useAbrirSolicitacao(ofertaId);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErro(null);

    const novoPrecoCentavos = brlParaCentavos(novoValorTexto);
    if (novoPrecoCentavos <= 0) {
      setErro('Informe um novo preço válido maior que zero.');
      return;
    }

    if (!justificativa.trim() || justificativa.trim().length < 5) {
      setErro('A justificativa é obrigatória e deve ter pelo menos 5 caracteres.');
      return;
    }

    abrirSolicitacaoMutation.mutate(
      {
        tipo: 'preco',
        novoPrecoCentavos,
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
            setErro('Erro ao enviar solicitação de alteração de preço.');
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
      aria-labelledby="modal-solicitar-preco-titulo"
    >
      <div className="w-full max-w-lg rounded-xl border border-border bg-surface p-6 shadow-xl">
        <h2 id="modal-solicitar-preco-titulo" className="text-lg font-bold text-neutral-900">
          Solicitar alteração de preço oficial
        </h2>
        <p className="mt-1 text-xs text-neutral-600">
          Como já existe um preço oficial vigente, a alteração passará por aprovação de um Responsável (RF-04).
          O valor atual continuará valendo até a decisão.
        </p>

        {precoAtualCentavos !== null && precoAtualCentavos !== undefined && (
          <div className="mt-3 rounded-lg bg-neutral-100 p-3 text-xs flex justify-between items-center text-neutral-700">
            <span>Preço oficial vigente:</span>
            <span className="font-bold font-mono text-neutral-900">{centavosParaBrl(precoAtualCentavos)}</span>
          </div>
        )}

        {erro && (
          <div className="mt-3 rounded-lg border border-danger/30 bg-danger/10 p-3 text-xs text-danger font-medium">
            {erro}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div>
            <label htmlFor="novoPreco" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
              Novo Preço Proposto (R$) *
            </label>
            <input
              id="novoPreco"
              type="text"
              placeholder="Ex: 84.500,00"
              value={novoValorTexto}
              onChange={(e) => setNovoValorTexto(e.target.value)}
              className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 font-mono focus:border-primary focus:outline-none"
              autoFocus
            />
          </div>

          <div>
            <label htmlFor="justificativaPreco" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
              Justificativa da Alteração *
            </label>
            <textarea
              id="justificativaPreco"
              rows={3}
              maxLength={500}
              placeholder="Explique o motivo do novo preço (ex: ajuste tabela FIPE, negociação, etc.)"
              value={justificativa}
              onChange={(e) => setJustificativa(e.target.value)}
              className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none"
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
