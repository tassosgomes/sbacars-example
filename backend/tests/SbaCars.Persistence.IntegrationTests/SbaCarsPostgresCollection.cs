using SbaCars.TestKit;
using Xunit;

namespace SbaCars.Persistence.IntegrationTests;

/// <summary>
/// Shares one <see cref="SbaCarsPostgresFixture"/> container across every test class in this
/// assembly: starting the container is the expensive part, and xUnit already serializes classes
/// within a collection, so there is no race between tests provisioning objects in different
/// schemas.
/// </summary>
/// <remarks>
/// <see cref="SbaCarsPostgresFixture"/> itself lives in <c>SbaCars.TestKit</c> (A9) since it has
/// no dependency on this assembly. This <c>[CollectionDefinition]</c>, however, stays local on
/// purpose: xUnit resolves a <c>[Collection(name)]</c> attribute against a definition in the
/// *same* test assembly (xunit.analyzers' xUnit1041 enforces this at compile time), so the
/// definition itself cannot be shared the way the fixture class can — every assembly that wants
/// the collection declares its own four-line registration.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class SbaCarsPostgresCollection : ICollectionFixture<SbaCarsPostgresFixture>
{
    public const string Name = "SbaCars Postgres";
}
