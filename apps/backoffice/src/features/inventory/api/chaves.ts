import type { ListarOfertasParams, ListarSolicitacoesParams } from '@/shared/api/types';

export const chaves = {
  ofertas: ['ofertas'] as const,
  listaOfertas: (params?: ListarOfertasParams) => ['ofertas', 'lista', params] as const,
  oferta: (id: string) => ['ofertas', id] as const,
  solicitacoes: ['solicitacoes'] as const,
  listaSolicitacoes: (params?: ListarSolicitacoesParams) => ['solicitacoes', 'lista', params] as const,
  solicitacao: (id: string) => ['solicitacoes', id] as const,
  contagemPendentes: ['solicitacoes', 'pendentes', 'contagem'] as const,
};
