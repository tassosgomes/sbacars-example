namespace SbaCars.Contracts.Interesse.V1;

/// <summary>
/// Published when D03 qualifies interest (Domain Doc <c>interesse.qualificado</c>). No PII — income,
/// documents, and contact data stay inside D03 feature boundaries.
/// </summary>
[IntegrationEvent("interesse.qualificado")]
public sealed record InteresseQualificadoIntegrationEvent(Guid InteresseId, DateTimeOffset OcorridoEm) : IIntegrationEvent;
