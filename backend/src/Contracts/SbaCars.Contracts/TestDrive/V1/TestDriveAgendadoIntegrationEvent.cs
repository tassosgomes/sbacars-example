namespace SbaCars.Contracts.TestDrive.V1;

/// <summary>
/// Published when D03 schedules a test drive (Domain Doc <c>testdrive.agendado</c>).
/// </summary>
[IntegrationEvent("testdrive.agendado")]
public sealed record TestDriveAgendadoIntegrationEvent(
    Guid SolicitacaoDeTestDriveId,
    DateTimeOffset OcorridoEm) : IIntegrationEvent;
