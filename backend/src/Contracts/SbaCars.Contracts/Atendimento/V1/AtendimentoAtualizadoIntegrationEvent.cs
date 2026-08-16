namespace SbaCars.Contracts.Atendimento.V1;

/// <summary>
/// Published when D03 updates an ongoing customer service case (Domain Doc <c>atendimento.atualizado</c>).
/// </summary>
[IntegrationEvent("atendimento.atualizado")]
public sealed record AtendimentoAtualizadoIntegrationEvent(Guid AtendimentoId, DateTimeOffset OcorridoEm) : IIntegrationEvent;
