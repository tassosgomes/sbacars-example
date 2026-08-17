import type { TipoSolicitacao } from '@/shared/api/types';

export interface BadgeTipoSolicitacaoProps {
  tipo: TipoSolicitacao;
  className?: string;
}

const configMap: Record<TipoSolicitacao, { label: string; bg: string; text: string }> = {
  elegibilidade: {
    label: 'ELEGIBILIDADE',
    bg: 'bg-emerald-100',
    text: 'text-emerald-900',
  },
  preco: {
    label: 'PREÇO',
    bg: 'bg-amber-100',
    text: 'text-amber-900',
  },
  retirada: {
    label: 'RETIRADA',
    bg: 'bg-neutral-200',
    text: 'text-neutral-900',
  },
  'reversao-venda': {
    label: 'REVERSÃO DE VENDA',
    bg: 'bg-purple-100',
    text: 'text-purple-900',
  },
};

export function BadgeTipoSolicitacao({ tipo, className = '' }: BadgeTipoSolicitacaoProps) {
  const config = configMap[tipo] ?? {
    label: tipo.toUpperCase(),
    bg: 'bg-neutral-100',
    text: 'text-neutral-800',
  };

  return (
    <span
      className={[
        'inline-flex items-center rounded px-2 py-0.5 text-[11px] font-bold tracking-wider uppercase',
        config.bg,
        config.text,
        className,
      ].join(' ')}
    >
      {config.label}
    </span>
  );
}
