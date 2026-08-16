namespace SbaCars.Contracts.Atendimento.V1;

/// <summary>
/// Published when D03 starts customer service for an interest (Domain Doc <c>atendimento.iniciado</c>).
/// </summary>
[IntegrationEvent("atendimento.iniciado")]
public sealed record AtendimentoIniciadoIntegrationEvent(Guid AtendimentoId, DateTimeOffset OcorridoEm) : IIntegrationEvent;
