/**
 * Limites e restrições de campos sincronizados com o OpenAPI Contract (api-contract.yaml).
 */
export const LIMITES = {
  placa: { min: 7, max: 8 },
  chassi: { min: 17, max: 17 },
  marca: { max: 50 },
  modelo: { max: 50 },
  versao: { max: 50 },
  anoMin: 1950,
  anoMax: 2100,
  cor: { max: 30 },
  combustivel: { max: 30 },
  cambio: { max: 30 },
  cep: { min: 8, max: 9 },
  cidade: { max: 100 },
  uf: { min: 2, max: 2 },
  descricaoFato: 1000,
  fonteFato: 200,
  limitacaoDeclarada: 500,
  justificativaSolicitacao: 500,
  observacaoDisponibilidade: 300,
  motivoRejeicao: 500,
} as const;
