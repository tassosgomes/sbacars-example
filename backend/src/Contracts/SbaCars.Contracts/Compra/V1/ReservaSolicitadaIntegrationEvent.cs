namespace SbaCars.Contracts.Compra.V1;

/// <summary>
/// Published when D04 requests inventory reservation (Domain Doc <c>compra.reserva-solicitada</c>, §2.5
/// foundation exception). Paired with <c>estoque.reserva-recusada</c> and acceptance via
/// <c>estoque.disponibilidade-alterada</c> with estado <c>reservado</c>.
/// </summary>
[IntegrationEvent("compra.reserva-solicitada")]
public sealed record ReservaSolicitadaIntegrationEvent(
    Guid OfertaId,
    Guid JornadaDeCompraId,
    DateTimeOffset OcorridoEm) : IIntegrationEvent;
