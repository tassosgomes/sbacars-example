using Xunit;

namespace SbaCars.Persistence.IntegrationTests;

/// <summary>
/// Shares one Postgres container across every test class in this assembly: starting the
/// container is the expensive part, and xUnit already serializes classes within a collection, so
/// there is no race between tests provisioning objects in different schemas.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SbaCarsPostgresCollection : ICollectionFixture<SbaCarsPostgresFixture>
{
    public const string Name = "SbaCars Postgres";
}
