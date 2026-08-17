import type { SituacaoOferta } from '@/shared/api/types';

export interface BadgeSituacaoProps {
  situacao: SituacaoOferta;
  className?: string;
}

const configMap: Record<SituacaoOferta, { label: string; bg: string; text: string; border: string }> = {
  'em-preparacao': {
    label: 'Em preparação',
    bg: 'bg-amber-50',
    text: 'text-amber-800',
    border: 'border-amber-200',
  },
  elegivel: {
    label: 'Elegível',
    bg: 'bg-emerald-50',
    text: 'text-emerald-800',
    border: 'border-emerald-200',
  },
  suspensa: {
    label: 'Suspensa',
    bg: 'bg-red-50',
    text: 'text-red-800',
    border: 'border-red-200',
  },
  retirada: {
    label: 'Retirada',
    bg: 'bg-neutral-100',
    text: 'text-neutral-700',
    border: 'border-neutral-300',
  },
};

export function BadgeSituacao({ situacao, className = '' }: BadgeSituacaoProps) {
  const config = configMap[situacao] ?? {
    label: situacao,
    bg: 'bg-neutral-100',
    text: 'text-neutral-700',
    border: 'border-neutral-200',
  };

  return (
    <span
      className={[
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold tracking-wide uppercase',
        config.bg,
        config.text,
        config.border,
        className,
      ].join(' ')}
    >
      {config.label}
    </span>
  );
}
