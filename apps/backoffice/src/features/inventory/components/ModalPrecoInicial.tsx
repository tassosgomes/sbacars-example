import { useState } from 'react';
import { Button } from '@sbacars/ui';
import { brlParaCentavos } from '@/shared/formatters/moeda';
import { useDefinirPrecoInicial } from '../api/useMutacoesOferta';
import { ApiError } from '@/shared/api/problemDetails';

export interface ModalPrecoInicialProps {
  isOpen: boolean;
  ofertaId: string;
  onClose: () => void;
  onSuccess?: () => void;
}

export function ModalPrecoInicial({ isOpen, ofertaId, onClose, onSuccess }: ModalPrecoInicialProps) {
  const [valorTexto, setValorTexto] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  const definirPrecoMutation = useDefinirPrecoInicial(ofertaId);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErro(null);

    const valorCentavos = brlParaCentavos(valorTexto);
    if (valorCentavos <= 0) {
      setErro('Informe um preço inicial válido maior que zero.');
      return;
    }

    definirPrecoMutation.mutate(
      { valorCentavos },
      {
        onSuccess: () => {
          onSuccess?.();
          onClose();
        },
        onError: (err) => {
          if (err instanceof ApiError) {
            setErro(err.problem.detail ?? err.problem.title);
          } else {
            setErro('Erro ao registrar preço oficial inicial.');
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
      aria-labelledby="modal-preco-inicial-titulo"
    >
      <div className="w-full max-w-md rounded-xl border border-border bg-surface p-6 shadow-xl">
        <h2 id="modal-preco-inicial-titulo" className="text-lg font-bold text-neutral-900">
          Definir preço oficial inicial
        </h2>
        <p className="mt-1 text-xs text-neutral-600">
          O primeiro preço oficial é cadastrado diretamente, sem necessidade de aprovação pela fila de validação (RF-04).
        </p>

        {erro && (
          <div className="mt-3 rounded-lg border border-danger/30 bg-danger/10 p-3 text-xs text-danger font-medium">
            {erro}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div>
            <label htmlFor="precoInicial" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
              Valor em Reais (R$) *
            </label>
            <input
              id="precoInicial"
              type="text"
              placeholder="Ex: 87.900,00"
              value={valorTexto}
              onChange={(e) => setValorTexto(e.target.value)}
              className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 font-mono focus:border-primary focus:outline-none"
              autoFocus
            />
          </div>

          <div className="flex justify-end gap-3 border-t border-border pt-4">
            <Button
              type="button"
              variant="secondary"
              onClick={onClose}
              disabled={definirPrecoMutation.isPending}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              disabled={definirPrecoMutation.isPending}
            >
              {definirPrecoMutation.isPending ? 'Gravando…' : 'Gravar preço'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
