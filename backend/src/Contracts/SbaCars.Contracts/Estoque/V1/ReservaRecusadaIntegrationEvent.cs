namespace SbaCars.Contracts.Estoque.V1;

/// <summary>
/// Published when D02 refuses a purchase reservation request (Domain Doc <c>estoque.reserva-recusada</c>,
/// §2.5). Acceptance is modeled by <see cref="DisponibilidadeAlteradaIntegrationEvent"/> with estado
/// <c>reservado</c>; this event is the explicit refusal path for the saga pair with
/// <c>compra.reserva-solicitada</c>.
/// </summary>
[IntegrationEvent("estoque.reserva-recusada")]
public sealed record ReservaRecusadaIntegrationEvent(
    Guid OfertaId,
    Guid JornadaDeCompraId,
    DateTimeOffset OcorridoEm) : IIntegrationEvent;
