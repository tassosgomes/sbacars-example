import { formatarIdadeRelativa } from '@/shared/formatters/data';

export interface IndicadorSlaProps {
  abertaEm?: string;
  foraDoSla?: boolean;
  className?: string;
}

export function IndicadorSla({ abertaEm, foraDoSla = false, className = '' }: IndicadorSlaProps) {
  const idade = formatarIdadeRelativa(abertaEm);

  if (foraDoSla) {
    return (
      <span
        title="Solicitação fora do SLA operacional de 1 dia útil"
        className={[
          'inline-flex items-center gap-1 rounded bg-red-100 border border-red-200 px-2 py-0.5 text-xs font-bold text-red-800',
          className,
        ].join(' ')}
      >
        <span>⚠️</span>
        <span>{idade}</span>
      </span>
    );
  }

  return (
    <span className={['text-xs text-neutral-600 font-medium', className].join(' ')}>
      {idade}
    </span>
  );
}
