namespace SbaCars.Contracts.TestDrive.V1;

/// <summary>
/// Published when D03 records a test-drive request (Domain Doc <c>testdrive.solicitado</c>).
/// </summary>
[IntegrationEvent("testdrive.solicitado")]
public sealed record TestDriveSolicitadoIntegrationEvent(
    Guid SolicitacaoDeTestDriveId,
    DateTimeOffset OcorridoEm) : IIntegrationEvent;
