namespace SbaCars.Contracts.Estoque.V1;

/// <summary>
/// Published when D02 changes operational availability of an offer (Domain Doc
/// <c>estoque.disponibilidade-alterada</c>). <see cref="Disponibilidade"/> carries the wire name of the
/// state (D02 RN-04: <c>disponível</c>, <c>reservado</c>, <c>vendido</c>) — not a C# enum, so the
/// contract stays stable across serializers.
/// </summary>
[IntegrationEvent("estoque.disponibilidade-alterada")]
public sealed record DisponibilidadeAlteradaIntegrationEvent(
    Guid OfertaId,
    string Disponibilidade,
    DateTimeOffset OcorridoEm) : IIntegrationEvent;
