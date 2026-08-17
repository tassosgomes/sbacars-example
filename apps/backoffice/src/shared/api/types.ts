import type { components, operations } from './schema';

type S = components['schemas'];

// Entidades e Resumos
export type OfertaResumo = S['OfertaResumo'];
export type OfertaDetalhe = S['OfertaDetalhe'];
export type OfertaResumoPaginado = S['OfertaResumoPaginado'];
export type SolicitacaoResumo = S['SolicitacaoResumo'];
export type SolicitacaoDetalhe = S['SolicitacaoDetalhe'];
export type SolicitacaoResumoPaginado = S['SolicitacaoResumoPaginado'];
export type ContagemPendentes = S['ContagemPendentes'];

// Enums e Literais
export type SituacaoOferta = S['SituacaoOferta'];
export type EstadoDisponibilidade = S['EstadoDisponibilidade'];
export type TipoSolicitacao = S['TipoSolicitacao'];
export type StatusSolicitacao = S['StatusSolicitacao'];
export type TipoVeiculo = S['TipoVeiculo'];
export type CodigoCriterio = S['CodigoCriterio'];
export type BlocoFatoTipo = S['BlocoFatoTipo'];

// Sub-estruturas
export type Veiculo = S['Veiculo'];
export type Autoria = S['Autoria'];
export type Localizacao = S['Localizacao'];
export type PrecoOficial = S['PrecoOficial'];
export type Disponibilidade = S['Disponibilidade'];
export type CriterioElegibilidade = S['CriterioElegibilidade'];
export type ChecklistElegibilidade = S['ChecklistElegibilidade'];
export type PendenciaResumo = S['PendenciaResumo'];
export type FatosConhecidos = S['FatosConhecidos'];
export type BlocoFato = S['BlocoFato'];
export type Evidencia = S['Evidencia'];
export type ContextoOferta = S['ContextoOferta'];
export type Decisao = S['Decisao'];

// Inputs / Payloads
export type VeiculoInput = S['VeiculoInput'];
export type VeiculoPatchInput = S['VeiculoPatchInput'];
export type FatosInput = S['FatosInput'];
export type BlocoFatoInput = S['BlocoFatoInput'];
export type DefinirPrecoInicialInput = S['DefinirPrecoInicialInput'];
export type AlterarDisponibilidadeInput = S['AlterarDisponibilidadeInput'];
export type AbrirSolicitacaoInput = S['AbrirSolicitacaoInput'];
export type AprovarSolicitacaoInput = S['AprovarSolicitacaoInput'];
export type RejeitarSolicitacaoInput = S['RejeitarSolicitacaoInput'];
export type UploadEvidenciaInput = S['UploadEvidenciaInput'];
export type UploadEvidenciaResponse = S['UploadEvidenciaResponse'];
export type DownloadEvidenciaResponse = S['DownloadEvidenciaResponse'];

// Erros
export type ProblemDetails = S['ProblemDetails'];
export type ProblemaSuspensao = S['ProblemaSuspensao'];

// Parâmetros de Query de Operações
export type ListarOfertasParams = operations['listarOfertas']['parameters']['query'];
export type ListarSolicitacoesParams = operations['listarSolicitacoes']['parameters']['query'];
