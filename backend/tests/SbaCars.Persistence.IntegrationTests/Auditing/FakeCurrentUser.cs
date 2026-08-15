using SbaCars.BuildingBlocks.Application;

namespace SbaCars.Persistence.IntegrationTests.Auditing;

/// <summary>
/// Minimal <see cref="ICurrentUser"/> test double: no HTTP context, no JWT — just the "who" the
/// audit trail needs. A6 will introduce the real claims-backed implementation.
/// </summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; init; } = true;

    public string? UserId { get; init; } = "operador-de-teste";

    public string? DisplayName { get; init; }

    public IReadOnlyCollection<string> Permissions { get; init; } = [];

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
}
