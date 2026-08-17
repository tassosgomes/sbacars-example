import { useEffect, useRef } from 'react';
import { Button } from '@sbacars/ui';
import type { CodigoCriterio } from '@/shared/api/types';

export interface DialogoSuspensaoProps {
  isOpen: boolean;
  criteriosAfetados?: CodigoCriterio[];
  onConfirmar: () => void;
  onCancelar: () => void;
  isLoading?: boolean;
}

const nomesCriterios: Record<string, string> = {
  identificacao: 'Identificação (placa)',
  'dados-basicos': 'Dados básicos do veículo',
  localizacao: 'Localização (cidade/UF)',
  'preco-oficial': 'Preço oficial',
  disponibilidade: 'Disponibilidade operacional',
  'transparencia-fatos': 'Transparência dos fatos conhecidos',
};

export function DialogoSuspensao({
  isOpen,
  criteriosAfetados = [],
  onConfirmar,
  onCancelar,
  isLoading = false,
}: DialogoSuspensaoProps) {
  const confirmBtnRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (isOpen) {
      confirmBtnRef.current?.focus();
    }
  }, [isOpen]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-xs animate-fade-in"
      role="dialog"
      aria-modal="true"
      aria-labelledby="dialogo-suspensao-titulo"
      aria-describedby="dialogo-suspensao-descricao"
    >
      <div className="w-full max-w-lg rounded-xl border border-border bg-surface p-6 shadow-xl">
        <div className="flex items-center gap-3 text-amber-800">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-amber-100 text-lg">
            ⚠️
          </div>
          <h2 id="dialogo-suspensao-titulo" className="text-base font-bold text-neutral-900">
            Esta alteração suspenderá a elegibilidade
          </h2>
        </div>

        <div id="dialogo-suspensao-descricao" className="mt-4 space-y-3 text-sm text-neutral-700">
          <p>
            A oferta atualmente está <strong>Elegível</strong> no catálogo. Ao salvar as alterações
            atuais, ela deixará de cumprir os seguintes critérios mínimos:
          </p>

          {criteriosAfetados.length > 0 && (
            <ul className="list-disc pl-5 space-y-1 text-xs text-red-700 font-medium">
              {criteriosAfetados.map((codigo) => (
                <li key={codigo}>{nomesCriterios[codigo] ?? codigo}</li>
              ))}
            </ul>
          )}

          <p className="rounded-md bg-amber-50 p-3 text-xs text-amber-900 border border-amber-200">
            Para que a oferta volte a ser elegível e publicada em D01, será necessária a correção dos
            dados e uma nova solicitação de validação aprovada pelo Responsável.
          </p>
        </div>

        <div className="mt-6 flex justify-end gap-3 border-t border-border pt-4">
          <Button
            type="button"
            variant="secondary"
            onClick={onCancelar}
            disabled={isLoading}
          >
            Cancelar
          </Button>
          <Button
            ref={confirmBtnRef}
            type="button"
            variant="danger"
            onClick={onConfirmar}
            disabled={isLoading}
          >
            {isLoading ? 'Salvando…' : 'Salvar e suspender'}
          </Button>
        </div>
      </div>
    </div>
  );
}
