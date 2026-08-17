import { requestJson } from '@/shared/api/client';
import type {
  OfertaResumoPaginado,
  OfertaDetalhe,
  SolicitacaoDetalhe,
  ListarOfertasParams,
  VeiculoInput,
  VeiculoPatchInput,
  DefinirPrecoInicialInput,
  FatosInput,
  AlterarDisponibilidadeInput,
  AbrirSolicitacaoInput,
} from '@/shared/api/types';

export async function listarOfertas(params?: ListarOfertasParams): Promise<OfertaResumoPaginado> {
  const query = new URLSearchParams();
  if (params) {
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    if (params.busca) query.set('busca', params.busca);
    if (params.situacao) {
      if (Array.isArray(params.situacao)) {
        params.situacao.forEach((s) => query.append('situacao', s));
      } else {
        query.append('situacao', params.situacao);
      }
    }
    if (params.disponibilidade) {
      if (Array.isArray(params.disponibilidade)) {
        params.disponibilidade.forEach((d) => query.append('disponibilidade', d));
      } else {
        query.append('disponibilidade', params.disponibilidade);
      }
    }
    if (params.uf) query.set('uf', params.uf);
    if (params.ordenarPor) query.set('ordenarPor', params.ordenarPor);
  }
  const qs = query.toString();
  return requestJson<OfertaResumoPaginado>(`/api/ofertas${qs ? `?${qs}` : ''}`);
}

export async function obterOferta(id: string): Promise<OfertaDetalhe> {
  return requestJson<OfertaDetalhe>(`/api/ofertas/${id}`);
}

export async function cadastrarVeiculo(input: VeiculoInput): Promise<OfertaDetalhe> {
  return requestJson<OfertaDetalhe>('/api/ofertas', {
    method: 'POST',
    body: JSON.stringify(input),
  });
}

export async function atualizarVeiculo(id: string, input: VeiculoPatchInput): Promise<OfertaDetalhe> {
  return requestJson<OfertaDetalhe>(`/api/ofertas/${id}/veiculo`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  });
}

export async function excluirOferta(id: string): Promise<void> {
  return requestJson<void>(`/api/ofertas/${id}`, {
    method: 'DELETE',
  });
}

export async function definirPrecoInicial(id: string, input: DefinirPrecoInicialInput): Promise<OfertaDetalhe> {
  return requestJson<OfertaDetalhe>(`/api/ofertas/${id}/preco`, {
    method: 'PUT',
    body: JSON.stringify(input),
  });
}

export async function substituirFatos(id: string, input: FatosInput): Promise<OfertaDetalhe> {
  return requestJson<OfertaDetalhe>(`/api/ofertas/${id}/fatos`, {
    method: 'PUT',
    body: JSON.stringify(input),
  });
}

export async function alterarDisponibilidade(
  id: string,
  input: AlterarDisponibilidadeInput
): Promise<OfertaDetalhe> {
  return requestJson<OfertaDetalhe>(`/api/ofertas/${id}/disponibilidade`, {
    method: 'POST',
    body: JSON.stringify(input),
  });
}

export async function abrirSolicitacao(id: string, input: AbrirSolicitacaoInput): Promise<SolicitacaoDetalhe> {
  return requestJson<SolicitacaoDetalhe>(`/api/ofertas/${id}/solicitacoes`, {
    method: 'POST',
    body: JSON.stringify(input),
  });
}
