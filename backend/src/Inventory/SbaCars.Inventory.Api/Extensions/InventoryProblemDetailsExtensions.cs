using FluentValidation;
using SbaCars.BuildingBlocks.Web.ErrorHandling;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Api.Extensions;

public static class InventoryProblemDetailsExtensions
{
    public static IServiceCollection AddInventoryProblemDetailsConfiguration(
        this IServiceCollection services) =>
        services.AddSbaCarsProblemDetails(exceptions => exceptions
            .Map<ValidationException>(StatusCodes.Status400BadRequest, "Requisição inválida.")
            .Map<PlacaDuplicadaException>(StatusCodes.Status409Conflict, "Já existe uma oferta ativa para a placa.")
            .Map<OfertaNaoEncontradaException>(StatusCodes.Status404NotFound, "Oferta não encontrada.")
            .Map<SolicitacaoNaoEncontradaException>(StatusCodes.Status404NotFound, "Solicitação não encontrada.")
            .Map<SolicitacaoJaDecididaException>(StatusCodes.Status409Conflict, "Solicitação já decidida.")
            .Map<AutoAprovacaoException>(StatusCodes.Status403Forbidden, "Aprovação não permitida.")
            .Map<JustificativaRejeicaoObrigatoriaException>(
                StatusCodes.Status400BadRequest,
                "Justificativa obrigatória.")
            .Map<ReversaoVendaDecisaoNaoDisponivelException>(
                StatusCodes.Status422UnprocessableEntity,
                "Reversão de venda ainda não está disponível neste fluxo.")
            .Map<PrecoJaDefinidoException>(StatusCodes.Status409Conflict, "Preço oficial já definido.")
            .Map<SolicitacaoPendenteDuplicadaException>(
                StatusCodes.Status409Conflict,
                "Solicitação pendente já existe.")
            .Map<CriteriosMinimosNaoAtendidosException>(
                StatusCodes.Status422UnprocessableEntity,
                "Critérios mínimos não atendidos.",
                (exception, problemDetails) =>
                {
                    problemDetails.Extensions["criteriosAfetados"] = exception.Criterios
                        .Select(criterio => criterio.ToContractValue())
                        .ToArray();
                })
            .Map<OfertaJaElegivelException>(StatusCodes.Status422UnprocessableEntity, "Oferta já elegível.")
            .Map<OfertaJaRetiradaException>(StatusCodes.Status422UnprocessableEntity, "Oferta já retirada.")
            .Map<PrecoVigenteNaoDefinidoException>(
                StatusCodes.Status422UnprocessableEntity,
                "Preço oficial vigente não definido.")
            .Map<ReversaoVendaNaoPermitidaException>(
                StatusCodes.Status422UnprocessableEntity,
                "Reversão de venda não permitida.")
            .Map<TransicaoInvalidaException>(
                StatusCodes.Status422UnprocessableEntity,
                "Transição de disponibilidade inválida.",
                (exception, problemDetails) =>
                {
                    problemDetails.Extensions["estadoAtual"] = exception.EstadoAtual.ToContractValue();
                    problemDetails.Extensions["novoEstado"] = exception.NovoEstado.ToContractValue();
                })
            .Map<EstadoDisponibilidadeNaoPermitidoException>(
                StatusCodes.Status422UnprocessableEntity,
                "Estado de disponibilidade não suportado.")
            .Map<TipoSolicitacaoNaoPermitidoException>(
                StatusCodes.Status422UnprocessableEntity,
                "Tipo de solicitação não suportado.")
            .Map<PrecoSolicitacaoObrigatorioException>(
                StatusCodes.Status422UnprocessableEntity,
                "Preço da solicitação obrigatório.")
            .Map<CampoPrecoSolicitacaoNaoPermitidoException>(
                StatusCodes.Status422UnprocessableEntity,
                "Campo de preço não permitido.")
            .Map<LimitacaoNaoDeclaradaException>(
                StatusCodes.Status422UnprocessableEntity,
                "Limitação não declarada.")
            .Map<SuspensaoNaoConfirmadaException>(
                StatusCodes.Status409Conflict,
                "Alteração suspenderia a elegibilidade.",
                (exception, problemDetails) =>
                {
                    problemDetails.Extensions["codigo"] = SuspensaoNaoConfirmadaException.Codigo;
                    problemDetails.Extensions["criteriosAfetados"] = exception.CriteriosAfetados
                        .Select(criterio => criterio.ToContractValue())
                        .ToArray();
                })
            .Map<PlacaImutavelException>(
                StatusCodes.Status422UnprocessableEntity,
                "A placa não pode ser alterada depois da preparação.")
            .Map<OfertaNaoExcluivelException>(
                StatusCodes.Status422UnprocessableEntity,
                "A oferta só pode ser excluída enquanto está em preparação.")
            .Map<ArquivoExcedeTamanhoException>(
                StatusCodes.Status413PayloadTooLarge,
                "O arquivo excede o tamanho máximo permitido.")
            .Map<TipoConteudoNaoAceitoException>(
                StatusCodes.Status415UnsupportedMediaType,
                "O tipo de conteúdo não é aceito.")
            .Map<EvidenciaNaoEncontradaException>(
                StatusCodes.Status404NotFound,
                "Evidência não encontrada."));
}
