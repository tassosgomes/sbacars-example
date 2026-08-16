namespace SbaCars.Contracts.Interesse.V1;

/// <summary>
/// Published when D03 records manifested interest (Domain Doc <c>interesse.manifestado</c>).
/// </summary>
[IntegrationEvent("interesse.manifestado")]
public sealed record InteresseManifestadoIntegrationEvent(
    Guid InteresseId,
    Guid ItemDoCatalogoId,
    DateTimeOffset OcorridoEm) : IIntegrationEvent;
