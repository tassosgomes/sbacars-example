import { Button } from '@sbacars/ui';

export interface ErrorStateProps {
  titulo?: string;
  mensagem?: string;
  traceId?: string;
  onRetry?: () => void;
}

export function ErrorState({
  titulo = 'Não foi possível carregar os dados',
  mensagem = 'Ocorreu um erro ao comunicar com o servidor. Tente novamente em alguns instantes.',
  traceId,
  onRetry,
}: ErrorStateProps) {
  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-danger/30 bg-danger/5 p-8 text-center">
      <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-danger/10 text-danger">
        <span className="text-xl font-bold">!</span>
      </div>
      <h3 className="text-base font-semibold text-neutral-900">{titulo}</h3>
      <p className="mt-1 max-w-md text-sm text-neutral-600">{mensagem}</p>
      {traceId && (
        <p className="mt-2 text-xs font-mono text-neutral-500">
          ID de rastreamento: <span className="select-all font-bold text-neutral-700">{traceId}</span>
        </p>
      )}
      {onRetry && (
        <div className="mt-5">
          <Button type="button" variant="secondary" size="sm" onClick={onRetry}>
            Tentar novamente
          </Button>
        </div>
      )}
    </div>
  );
}
