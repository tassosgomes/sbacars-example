import { useMutation, useQueryClient } from '@tanstack/react-query';
import { chaves } from './chaves';
import { aprovarSolicitacao, rejeitarSolicitacao } from './solicitacoes';
import type { AprovarSolicitacaoInput, RejeitarSolicitacaoInput } from '@/shared/api/types';

export function useAprovarSolicitacao() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input?: AprovarSolicitacaoInput }) =>
      aprovarSolicitacao(id, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chaves.solicitacoes });
      qc.invalidateQueries({ queryKey: chaves.ofertas });
    },
  });
}

export function useRejeitarSolicitacao() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: RejeitarSolicitacaoInput }) =>
      rejeitarSolicitacao(id, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chaves.solicitacoes });
      qc.invalidateQueries({ queryKey: chaves.ofertas });
    },
  });
}
