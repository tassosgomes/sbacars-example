// Pages
export { ListaEstoquePage } from './pages/ListaEstoquePage';
export { CadastroVeiculoPage } from './pages/CadastroVeiculoPage';
export { DetalheOfertaPage } from './pages/DetalheOfertaPage';
export { FatosConhecidosPage } from './pages/FatosConhecidosPage';
export { FilaValidacaoPage } from './pages/FilaValidacaoPage';
export { DetalheSolicitacaoPage } from './pages/DetalheSolicitacaoPage';

// Components
export { BadgeSituacao } from './components/BadgeSituacao';
export { BadgeDisponibilidade } from './components/BadgeDisponibilidade';
export { BadgeTipoSolicitacao } from './components/BadgeTipoSolicitacao';
export { ChecklistElegibilidade } from './components/ChecklistElegibilidade';
export { DialogoSuspensao } from './components/DialogoSuspensao';
export { IndicadorSla } from './components/IndicadorSla';
export { SeloLimitacao } from './components/SeloLimitacao';
export { ValorComProcedencia } from './components/ValorComProcedencia';
export { UploadEvidencia } from './components/UploadEvidencia';

// Hooks & API
export { useListarOfertas, useObterOferta } from './api/useOfertas';
export {
  useCadastrarVeiculo,
  useAtualizarVeiculo,
  useExcluirOferta,
  useDefinirPrecoInicial,
  useSubstituirFatos,
  useAlterarDisponibilidade,
  useAbrirSolicitacao,
} from './api/useMutacoesOferta';
export {
  useFilaValidacao,
  useDetalheSolicitacao,
  useContagemPendentes,
} from './api/useSolicitacoes';
export { useAprovarSolicitacao, useRejeitarSolicitacao } from './api/useDecisao';
export { useMutacaoComSuspensao } from './api/useMutacaoComSuspensao';
