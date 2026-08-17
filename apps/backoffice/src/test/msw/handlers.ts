import { http, HttpResponse } from 'msw';
import type {
  OfertaDetalhe,
  OfertaResumoPaginado,
  SolicitacaoDetalhe,
  SolicitacaoResumoPaginado,
  ContagemPendentes,
} from '@/shared/api/types';

export const mockOfertaDetalhe: OfertaDetalhe = {
  ofertaId: '3f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c47',
  situacao: 'elegivel',
  criadaEm: '2026-08-10T14:30:00Z',
  atualizadoEm: '2026-08-12T10:15:00Z',
  motivoSuspensao: null,
  suspensaEm: null,
  veiculo: {
    tipoVeiculo: 'carro-seminovo',
    placa: 'ABC1D23',
    chassi: '93HFC2650MZ204817',
    marca: 'Honda',
    modelo: 'Civic',
    versao: 'EXL 2.0',
    anoFabricacao: 2021,
    anoModelo: 2022,
    quilometragem: 48300,
    cor: 'Prata',
    combustivel: 'Flex',
    cambio: 'Automático',
    localizacao: {
      cep: '13010-111',
      cidade: 'Campinas',
      uf: 'SP',
    },
  },
  fatos: {
    origem: {
      tipo: 'origem',
      indisponivel: false,
      descricao: 'Veículo de frota corporativa, único proprietário pessoa jurídica.',
      fonte: 'Contrato de cessão Localiza, 02/2026',
      evidencia: {
        evidenciaId: 'e1a2b3c4-d5e6-7f80-1234-56789abcdef0',
        nomeArquivo: 'laudo.pdf',
        tipoConteudo: 'application/pdf',
        tamanhoBytes: 102400,
        enviadaEm: '2026-08-11T09:00:00Z',
      },
      limitacaoDeclarada: null,
      atendeTransparencia: true,
      atualizadoPor: {
        usuarioId: '11111111-1111-1111-1111-111111111111',
        nome: 'Operador Teste',
        em: '2026-08-11T09:00:00Z',
      },
    },
    condicao: {
      tipo: 'condicao',
      indisponivel: false,
      descricao: 'Revisões em concessionária até 40.000 km. Pneus dianteiros novos.',
      fonte: 'Histórico de manutenção Honda',
      evidencia: null,
      limitacaoDeclarada: null,
      atendeTransparencia: true,
      atualizadoPor: {
        usuarioId: '11111111-1111-1111-1111-111111111111',
        nome: 'Operador Teste',
        em: '2026-08-11T09:10:00Z',
      },
    },
    historico: {
      tipo: 'historico',
      indisponivel: true,
      descricao: null,
      fonte: null,
      evidencia: null,
      limitacaoDeclarada: 'Não foi possível obter o histórico de sinistros deste veículo junto às bases consultadas.',
      atendeTransparencia: true,
      atualizadoPor: {
        usuarioId: '11111111-1111-1111-1111-111111111111',
        nome: 'Operador Teste',
        em: '2026-08-11T09:15:00Z',
      },
    },
  },
  precoOficial: {
    valorCentavos: 8790000,
    moeda: 'BRL',
    definidoPor: {
      usuarioId: '11111111-1111-1111-1111-111111111111',
      nome: 'Ana Souza',
      em: '2026-08-12T10:15:00Z',
    },
  },
  disponibilidade: {
    estado: 'disponivel',
    desde: '2026-08-10T14:30:00Z',
    alteradaPor: {
      usuarioId: '11111111-1111-1111-1111-111111111111',
      nome: 'Operador Teste',
      em: '2026-08-10T14:30:00Z',
    },
    transicoesPermitidas: ['reservado', 'vendido'],
  },
  elegibilidade: {
    atendidos: 6,
    total: 6,
    criterios: [
      { codigo: 'identificacao', atendido: true, pendencia: null },
      { codigo: 'dados-basicos', atendido: true, pendencia: null },
      { codigo: 'localizacao', atendido: true, pendencia: null },
      { codigo: 'preco-oficial', atendido: true, pendencia: null },
      { codigo: 'disponibilidade', atendido: true, pendencia: null },
      { codigo: 'transparencia-fatos', atendido: true, pendencia: null },
    ],
    podeSolicitarElegibilidade: false,
  },
  pendencias: [],
};

export const mockOfertasLista: OfertaResumoPaginado = {
  items: [
    {
      ofertaId: '3f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c47',
      placa: 'ABC1D23',
      descricaoVeiculo: 'Honda Civic EXL 2.0',
      anoFabricacao: 2021,
      anoModelo: 2022,
      quilometragem: 48300,
      localizacao: { cidade: 'Campinas', uf: 'SP' },
      precoOficialCentavos: 8790000,
      situacao: 'elegivel',
      disponibilidade: 'disponivel',
      pendencias: [],
      atualizadoEm: '2026-08-12T10:15:00Z',
    },
    {
      ofertaId: '4f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c48',
      placa: 'XYZ9876',
      descricaoVeiculo: 'Toyota Corolla XEi 2.0',
      anoFabricacao: 2020,
      anoModelo: 2021,
      quilometragem: 62000,
      localizacao: { cidade: 'São Paulo', uf: 'SP' },
      precoOficialCentavos: null,
      situacao: 'em-preparacao',
      disponibilidade: 'disponivel',
      pendencias: ['preco'],
      atualizadoEm: '2026-08-15T11:00:00Z',
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 2,
  totalPages: 1,
  hasNextPage: false,
  hasPreviousPage: false,
};

export const mockSolicitacoesLista: SolicitacaoResumoPaginado = {
  items: [
    {
      solicitacaoId: '9c1f7e28-4b6a-4d5f-8a09-2c3e5b7d1f40',
      ofertaId: '3f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c47',
      placa: 'ABC-1234',
      descricaoVeiculo: 'Toyota Corolla XEI 2021/2022',
      tipo: 'preco',
      status: 'pendente',
      valorVigente: 'R$ 115.000',
      valorProposto: 'R$ 112.500',
      abertaEm: '2026-08-16T10:00:00Z',
      abertaPor: {
        usuarioId: '22222222-2222-2222-2222-222222222222',
        nome: 'João Pereira',
        em: '2026-08-16T10:00:00Z',
      },
      foraDoSla: false,
    },
    {
      solicitacaoId: '8c1f7e28-4b6a-4d5f-8a09-2c3e5b7d1f41',
      ofertaId: '4f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c48',
      placa: 'XYZ-9876',
      descricaoVeiculo: 'Jeep Compass Longitude 2023/2023',
      tipo: 'elegibilidade',
      status: 'pendente',
      valorVigente: 'Em preparação',
      valorProposto: 'Elegível',
      abertaEm: '2026-08-15T08:00:00Z',
      abertaPor: {
        usuarioId: '33333333-3333-3333-3333-333333333333',
        nome: 'Maria Costa',
        em: '2026-08-15T08:00:00Z',
      },
      foraDoSla: true,
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 2,
  totalPages: 1,
  hasNextPage: false,
  hasPreviousPage: false,
};

export const mockSolicitacaoDetalhe: SolicitacaoDetalhe = {
  solicitacaoId: '9c1f7e28-4b6a-4d5f-8a09-2c3e5b7d1f40',
  ofertaId: '3f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c47',
  placa: 'ABC-1234',
  descricaoVeiculo: 'Toyota Corolla XEI 2021/2022',
  tipo: 'preco',
  status: 'pendente',
  valorVigente: 'R$ 115.000',
  valorProposto: 'R$ 112.500',
  justificativa: 'Ajuste de preço de acordo com a tabela FIPE atualizada.',
  abertaEm: '2026-08-16T10:00:00Z',
  abertaPor: {
    usuarioId: '22222222-2222-2222-2222-222222222222',
    nome: 'João Pereira',
    em: '2026-08-16T10:00:00Z',
  },
  foraDoSla: false,
  podeDecidir: true,
  impactoAoAprovar: 'Ao aprovar, o preço oficial passa a ser R$ 112.500,00 e será atualizado no catálogo.',
  novoPrecoCentavos: 11250000,
  contextoOferta: {
    situacao: 'elegivel',
    disponibilidade: 'disponivel',
    precoOficial: {
      valorCentavos: 11500000,
      moeda: 'BRL',
      definidoPor: {
        usuarioId: '11111111-1111-1111-1111-111111111111',
        nome: 'Operador',
        em: '2026-08-10T10:00:00Z',
      },
    },
    localizacao: { cidade: 'Campinas', uf: 'SP' },
    blocosComLimitacao: ['historico'],
  },
  decisao: null,
};

export const mockContagemPendentes: ContagemPendentes = {
  total: 2,
  foraDoSla: 1,
  porTipo: {
    preco: 1,
    elegibilidade: 1,
    retirada: 0,
    'reversao-venda': 0,
  },
};

export const handlers = [
  // Ofertas
  http.get('*/api/ofertas', () => {
    return HttpResponse.json(mockOfertasLista);
  }),
  http.get('*/api/ofertas/:id', ({ params }) => {
    return HttpResponse.json({ ...mockOfertaDetalhe, ofertaId: params.id as string });
  }),
  http.post('*/api/ofertas', async ({ request }) => {
    const body = (await request.json()) as Record<string, unknown>;
    return HttpResponse.json({
      ...mockOfertaDetalhe,
      ofertaId: '5f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c49',
      situacao: 'em-preparacao',
      veiculo: { ...mockOfertaDetalhe.veiculo, ...body },
    }, { status: 201 });
  }),
  http.patch('*/api/ofertas/:id/veiculo', async ({ request, params }) => {
    const body = (await request.json()) as Record<string, unknown>;
    return HttpResponse.json({
      ...mockOfertaDetalhe,
      ofertaId: params.id as string,
      veiculo: { ...mockOfertaDetalhe.veiculo, ...body },
    });
  }),
  http.put('*/api/ofertas/:id/fatos', async ({ request, params }) => {
    const body = (await request.json()) as Record<string, unknown>;
    return HttpResponse.json({
      ...mockOfertaDetalhe,
      ofertaId: params.id as string,
      fatos: { ...mockOfertaDetalhe.fatos, ...body },
    });
  }),
  http.put('*/api/ofertas/:id/preco', async ({ request, params }) => {
    const body = (await request.json()) as { valorCentavos: number };
    return HttpResponse.json({
      ...mockOfertaDetalhe,
      ofertaId: params.id as string,
      precoOficial: {
        valorCentavos: body.valorCentavos,
        moeda: 'BRL',
        definidoPor: { usuarioId: '11111111-1111-1111-1111-111111111111', nome: 'Operador', em: new Date().toISOString() },
      },
    });
  }),
  http.post('*/api/ofertas/:id/disponibilidade', async ({ request, params }) => {
    const body = (await request.json()) as { novoEstado: 'disponivel' | 'reservado' | 'vendido' };
    return HttpResponse.json({
      ...mockOfertaDetalhe,
      ofertaId: params.id as string,
      disponibilidade: {
        ...mockOfertaDetalhe.disponibilidade,
        estado: body.novoEstado,
      },
    });
  }),
  http.post('*/api/ofertas/:id/solicitacoes', async ({ request }) => {
    const body = (await request.json()) as Record<string, unknown>;
    return HttpResponse.json({
      ...mockSolicitacaoDetalhe,
      ...body,
    }, { status: 201 });
  }),
  http.delete('*/api/ofertas/:id', () => {
    return new HttpResponse(null, { status: 204 });
  }),

  // Solicitações
  http.get('*/api/solicitacoes', () => {
    return HttpResponse.json(mockSolicitacoesLista);
  }),
  http.get('*/api/solicitacoes/pendentes/contagem', () => {
    return HttpResponse.json(mockContagemPendentes);
  }),
  http.get('*/api/solicitacoes/:id', ({ params }) => {
    return HttpResponse.json({ ...mockSolicitacaoDetalhe, solicitacaoId: params.id as string });
  }),
  http.post('*/api/solicitacoes/:id/aprovar', ({ params }) => {
    return HttpResponse.json({
      ...mockSolicitacaoDetalhe,
      solicitacaoId: params.id as string,
      status: 'aprovada',
      decisao: {
        status: 'aprovada',
        decididaEm: new Date().toISOString(),
        decididaPor: { usuarioId: '99999999-9999-9999-9999-999999999999', nome: 'Responsável', em: new Date().toISOString() },
        justificativa: null,
      },
    });
  }),
  http.post('*/api/solicitacoes/:id/rejeitar', async ({ request, params }) => {
    const body = (await request.json()) as { justificativa: string };
    return HttpResponse.json({
      ...mockSolicitacaoDetalhe,
      solicitacaoId: params.id as string,
      status: 'rejeitada',
      decisao: {
        status: 'rejeitada',
        decididaEm: new Date().toISOString(),
        decididaPor: { usuarioId: '99999999-9999-9999-9999-999999999999', nome: 'Responsável', em: new Date().toISOString() },
        justificativa: body.justificativa,
      },
    });
  }),
];
