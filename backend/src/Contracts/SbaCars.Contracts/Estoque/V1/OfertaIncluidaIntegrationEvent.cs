namespace SbaCars.Contracts.Estoque.V1;

/// <summary>
/// Published when D02 includes a new curated offer in inventory (Domain Doc <c>estoque.oferta-incluida</c>).
/// Payload is intentionally lean — no vehicle, price, or document fields; those belong to feature PRDs.
/// </summary>
[IntegrationEvent("estoque.oferta-incluida")]
public sealed record OfertaIncluidaIntegrationEvent(Guid OfertaId, DateTimeOffset OcorridoEm) : IIntegrationEvent;
