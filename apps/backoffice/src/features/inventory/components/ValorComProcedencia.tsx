import type { ReactNode } from 'react';
import type { Autoria } from '@/shared/api/types';
import { formatarDataHora } from '@/shared/formatters/data';

export interface ValorComProcedenciaProps {
  valor: ReactNode;
  autoria?: Autoria | null;
  label?: string;
  className?: string;
}

export function ValorComProcedencia({
  valor,
  autoria,
  label,
  className = '',
}: ValorComProcedenciaProps) {
  return (
    <div className={['flex flex-col gap-0.5', className].join(' ')}>
      {label && <span className="text-xs font-bold uppercase tracking-wider text-muted">{label}</span>}
      <div className="text-base font-semibold text-neutral-900">{valor}</div>
      {autoria && (
        <span className="text-xs text-muted">
          Atualizado em {formatarDataHora(autoria.em)} por {autoria.nome}
        </span>
      )}
    </div>
  );
}
