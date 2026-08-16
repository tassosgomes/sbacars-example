namespace SbaCars.Contracts.Catalogo.V1;

/// <summary>
/// Published when D01 updates a catalog item sourced from D02 (Domain Doc <c>catalogo.item-atualizado</c>).
/// </summary>
[IntegrationEvent("catalogo.item-atualizado")]
public sealed record ItemAtualizadoIntegrationEvent(
    Guid ItemDoCatalogoId,
    Guid OfertaId,
    DateTimeOffset OcorridoEm) : IIntegrationEvent;
