import { useState, useCallback } from 'react';
import type { UseMutationResult, MutateOptions } from '@tanstack/react-query';
import { ApiError } from '@/shared/api/problemDetails';
import type { CodigoCriterio, ProblemaSuspensao } from '@/shared/api/types';

export interface SuspensaoPendente<TVariables> {
  criteriosAfetados: CodigoCriterio[];
  variables: TVariables;
}

export function useMutacaoComSuspensao<
  TData,
  TVariables extends { confirmaSuspensao?: boolean }
>(mutation: UseMutationResult<TData, Error, TVariables>) {
  const [suspensaoPendente, setSuspensaoPendente] = useState<SuspensaoPendente<TVariables> | null>(null);

  const mutate = useCallback(
    (
      variables: Omit<TVariables, 'confirmaSuspensao'> | TVariables,
      options?: MutateOptions<TData, Error, TVariables, unknown>
    ) => {
      const varsWithFlag = { ...variables, confirmaSuspensao: false } as TVariables;
      mutation.mutate(varsWithFlag, {
        ...options,
        onError: (err, vars, context) => {
          if (err instanceof ApiError && err.status === 409) {
            const problem = err.problem as ProblemaSuspensao;
            if (problem.codigo === 'suspensao-nao-confirmada') {
              setSuspensaoPendente({
                criteriosAfetados: problem.criteriosAfetados ?? [],
                variables: vars,
              });
              return;
            }
          }
          if (options?.onError) {
            (options.onError as (err: Error, vars: TVariables, ctx: unknown) => void)(err, vars, context);
          }
        },
      });
    },
    [mutation]
  );

  const confirmarSuspensao = useCallback(
    (options?: MutateOptions<TData, Error, TVariables, unknown>) => {
      if (!suspensaoPendente) return;
      const { variables } = suspensaoPendente;
      setSuspensaoPendente(null);
      mutation.mutate(
        { ...variables, confirmaSuspensao: true },
        options
      );
    },
    [mutation, suspensaoPendente]
  );

  const cancelarSuspensao = useCallback(() => {
    setSuspensaoPendente(null);
  }, []);

  return {
    ...mutation,
    mutate,
    suspensaoPendente,
    confirmarSuspensao,
    cancelarSuspensao,
  };
}
