using System.ComponentModel.DataAnnotations;

namespace SbaCars.BuildingBlocks.Persistence;

/// <summary>
/// Connection string for a service's own <see cref="SbaCarsDbContext"/>, bound through the
/// Options Pattern with <c>ValidateOnStart</c> (§4.4 of the foundation plan): a missing or empty
/// connection string fails the process at boot, never on the first request.
/// </summary>
/// <remarks>
/// Reused as-is by every service: each service is a separate process with its own configuration
/// and its own DI container, so there is no collision between, say, <c>catalog-service</c>'s
/// <c>Persistence:ConnectionString</c> and <c>inventory-service</c>'s — they are different
/// configuration files entirely. A Migrator binds the same type to the same section, but to the
/// schema-owning role's connection string instead of the application role's.
/// </remarks>
public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;
}
