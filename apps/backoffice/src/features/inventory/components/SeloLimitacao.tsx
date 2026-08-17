export interface SeloLimitacaoProps {
  texto?: string | null;
  className?: string;
}

export function SeloLimitacao({ texto, className = '' }: SeloLimitacaoProps) {
  return (
    <div className={['flex flex-col gap-1 rounded-md bg-amber-50 border border-amber-200 p-3', className].join(' ')}>
      <div className="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-amber-900">
        <span className="text-amber-700">⚠️</span>
        <span>Limitação declarada</span>
      </div>
      {texto && <p className="text-xs text-amber-950 font-normal leading-relaxed">{texto}</p>}
    </div>
  );
}
