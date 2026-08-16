namespace SbaCars.Contracts.Estoque.V1;

/// <summary>
/// Published when D02 withdraws a curated offer from inventory (Domain Doc <c>estoque.oferta-retirada</c>).
/// </summary>
[IntegrationEvent("estoque.oferta-retirada")]
public sealed record OfertaRetiradaIntegrationEvent(Guid OfertaId, DateTimeOffset OcorridoEm) : IIntegrationEvent;
