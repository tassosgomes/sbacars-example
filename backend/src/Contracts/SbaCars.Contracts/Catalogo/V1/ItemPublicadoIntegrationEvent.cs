namespace SbaCars.Contracts.Catalogo.V1;

/// <summary>
/// Published when D01 publishes a catalog item sourced from D02 (Domain Doc <c>catalogo.item-publicado</c>).
/// </summary>
[IntegrationEvent("catalogo.item-publicado")]
public sealed record ItemPublicadoIntegrationEvent(
    Guid ItemDoCatalogoId,
    Guid OfertaId,
    DateTimeOffset OcorridoEm) : IIntegrationEvent;
