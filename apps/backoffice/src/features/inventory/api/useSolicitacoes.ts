import { useQuery } from '@tanstack/react-query';
import { chaves } from './chaves';
import { contarSolicitacoesPendentes, listarSolicitacoes, obterSolicitacao } from './solicitacoes';
import type { ListarSolicitacoesParams } from '@/shared/api/types';

export function useContagemPendentes(enabled = true) {
  return useQuery({
    queryKey: chaves.contagemPendentes,
    queryFn: contarSolicitacoesPendentes,
    enabled,
    refetchInterval: 60_000,
    staleTime: 30_000,
  });
}

export function useFilaValidacao(params?: ListarSolicitacoesParams) {
  return useQuery({
    queryKey: chaves.listaSolicitacoes(params),
    queryFn: () => listarSolicitacoes(params),
    staleTime: 0, // Dado quente de decisão
  });
}

export function useDetalheSolicitacao(id?: string) {
  return useQuery({
    queryKey: chaves.solicitacao(id ?? ''),
    queryFn: () => obterSolicitacao(id!),
    enabled: !!id,
    staleTime: 0,
  });
}
