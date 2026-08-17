import type { ChecklistElegibilidade as ChecklistType } from '@/shared/api/types';

export interface ChecklistElegibilidadeProps {
  checklist?: ChecklistType | null;
  className?: string;
  onCriterioClick?: (codigo: string) => void;
}

const nomesCriterios: Record<string, string> = {
  identificacao: 'CM-1: Identificação (placa)',
  'dados-basicos': 'CM-2: Dados básicos (marca, modelo, ano, km, câmbio)',
  localizacao: 'CM-3: Localização (cidade e UF)',
  'preco-oficial': 'CM-4: Preço oficial vigente',
  disponibilidade: 'CM-5: Disponibilidade conhecida',
  'transparencia-fatos': 'CM-6: Transparência dos fatos (conteúdo ou limitação)',
};

export function ChecklistElegibilidade({
  checklist,
  className = '',
  onCriterioClick,
}: ChecklistElegibilidadeProps) {
  if (!checklist) {
    return null;
  }

  const atendidos = checklist.atendidos;
  const total = checklist.total;
  const isCompleto = atendidos === total;

  return (
    <div className={['flex flex-col gap-3 rounded-xl border border-border bg-surface p-5', className].join(' ')}>
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-bold uppercase tracking-wider text-neutral-900">
          Critérios de Elegibilidade
        </h3>
        <span
          className={[
            'rounded-full px-2.5 py-0.5 text-xs font-bold',
            isCompleto ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800',
          ].join(' ')}
        >
          {atendidos} de {total} atendidos
        </span>
      </div>

      <div className="h-1.5 w-full overflow-hidden rounded-full bg-neutral-200">
        <div
          className={[
            'h-full transition-all duration-300',
            isCompleto ? 'bg-emerald-600' : 'bg-amber-500',
          ].join(' ')}
          style={{ width: `${(atendidos / total) * 100}%` }}
        />
      </div>

      <ul className="mt-2 divide-y divide-border/60">
        {checklist.criterios.map((c) => {
          const nome = nomesCriterios[c.codigo] ?? c.codigo;
          return (
            <li
              key={c.codigo}
              onClick={() => onCriterioClick?.(c.codigo)}
              className={[
                'flex items-start gap-2.5 py-2 text-xs',
                onCriterioClick ? 'cursor-pointer hover:bg-neutral-50 rounded px-1 -mx-1' : '',
              ].join(' ')}
            >
              <span
                className={[
                  'mt-0.5 flex h-4 w-4 shrink-0 items-center justify-center rounded-full text-[10px] font-bold',
                  c.atendido ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-800',
                ].join(' ')}
              >
                {c.atendido ? '✓' : '✗'}
              </span>
              <div className="flex flex-col">
                <span
                  className={[
                    'font-medium',
                    c.atendido ? 'text-neutral-800' : 'text-neutral-900 font-semibold',
                  ].join(' ')}
                >
                  {nome}
                </span>
                {!c.atendido && c.pendencia && (
                  <span className="text-[11px] text-red-600 mt-0.5">{c.pendencia}</span>
                )}
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
