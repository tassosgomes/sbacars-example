import { requestJson } from '@/shared/api/client';
import type {
  SolicitacaoResumoPaginado,
  SolicitacaoDetalhe,
  ContagemPendentes,
  ListarSolicitacoesParams,
  AprovarSolicitacaoInput,
  RejeitarSolicitacaoInput,
} from '@/shared/api/types';

export async function listarSolicitacoes(params?: ListarSolicitacoesParams): Promise<SolicitacaoResumoPaginado> {
  const query = new URLSearchParams();
  if (params) {
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    if (params.status) query.set('status', params.status);
    if (params.tipo) {
      if (Array.isArray(params.tipo)) {
        params.tipo.forEach((t) => query.append('tipo', t));
      } else {
        query.append('tipo', params.tipo);
      }
    }
    if (params.ordenarPor) query.set('ordenarPor', params.ordenarPor);
  }
  const qs = query.toString();
  return requestJson<SolicitacaoResumoPaginado>(`/api/solicitacoes${qs ? `?${qs}` : ''}`);
}

export async function contarSolicitacoesPendentes(): Promise<ContagemPendentes> {
  return requestJson<ContagemPendentes>('/api/solicitacoes/pendentes/contagem');
}

export async function obterSolicitacao(id: string): Promise<SolicitacaoDetalhe> {
  return requestJson<SolicitacaoDetalhe>(`/api/solicitacoes/${id}`);
}

export async function aprovarSolicitacao(
  id: string,
  input?: AprovarSolicitacaoInput
): Promise<SolicitacaoDetalhe> {
  return requestJson<SolicitacaoDetalhe>(`/api/solicitacoes/${id}/aprovar`, {
    method: 'POST',
    body: input ? JSON.stringify(input) : undefined,
  });
}

export async function rejeitarSolicitacao(
  id: string,
  input: RejeitarSolicitacaoInput
): Promise<SolicitacaoDetalhe> {
  return requestJson<SolicitacaoDetalhe>(`/api/solicitacoes/${id}/rejeitar`, {
    method: 'POST',
    body: JSON.stringify(input),
  });
}
