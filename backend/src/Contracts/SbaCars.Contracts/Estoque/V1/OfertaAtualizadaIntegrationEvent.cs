namespace SbaCars.Contracts.Estoque.V1;

/// <summary>
/// Published when D02 updates an existing curated offer (Domain Doc <c>estoque.oferta-atualizada</c>).
/// </summary>
[IntegrationEvent("estoque.oferta-atualizada")]
public sealed record OfertaAtualizadaIntegrationEvent(Guid OfertaId, DateTimeOffset OcorridoEm) : IIntegrationEvent;
