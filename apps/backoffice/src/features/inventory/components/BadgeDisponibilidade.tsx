import type { EstadoDisponibilidade } from '@/shared/api/types';

export interface BadgeDisponibilidadeProps {
  disponibilidade: EstadoDisponibilidade;
  className?: string;
}

const configMap: Record<EstadoDisponibilidade, { label: string; bg: string; text: string; border: string }> = {
  disponivel: {
    label: 'Disponível',
    bg: 'bg-emerald-50',
    text: 'text-emerald-800',
    border: 'border-emerald-200',
  },
  reservado: {
    label: 'Reservado',
    bg: 'bg-blue-50',
    text: 'text-blue-800',
    border: 'border-blue-200',
  },
  vendido: {
    label: 'Vendido',
    bg: 'bg-neutral-100',
    text: 'text-neutral-700',
    border: 'border-neutral-300',
  },
};

export function BadgeDisponibilidade({ disponibilidade, className = '' }: BadgeDisponibilidadeProps) {
  const config = configMap[disponibilidade] ?? {
    label: disponibilidade,
    bg: 'bg-neutral-100',
    text: 'text-neutral-700',
    border: 'border-neutral-200',
  };

  return (
    <span
      className={[
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold tracking-wide',
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
