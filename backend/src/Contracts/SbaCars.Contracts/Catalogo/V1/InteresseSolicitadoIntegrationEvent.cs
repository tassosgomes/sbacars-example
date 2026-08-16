namespace SbaCars.Contracts.Catalogo.V1;

/// <summary>
/// Published when D01 records interest solicited from the catalog (Domain Doc
/// <c>catalogo.interesse-solicitado</c>). <see cref="ItemDoCatalogoId"/> is the documented cross-context
/// reference into D01.
/// </summary>
[IntegrationEvent("catalogo.interesse-solicitado")]
public sealed record InteresseSolicitadoIntegrationEvent(
    Guid InteresseId,
    Guid ItemDoCatalogoId,
    DateTimeOffset OcorridoEm) : IIntegrationEvent;
