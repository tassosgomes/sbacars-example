using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class PrecoSolicitacaoObrigatorioException()
    : DomainException("novoPrecoCentavos é obrigatório para uma solicitação de preço.");
