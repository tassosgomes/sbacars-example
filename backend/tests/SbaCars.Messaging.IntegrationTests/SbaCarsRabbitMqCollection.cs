namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// Shares one <see cref="SbaCarsRabbitMqFixture"/> container across every test class in this
/// assembly — starting the broker is the expensive part, and each test class binds its own,
/// distinct input/error queue names, so there is no cross-test interference within the shared
/// container.
/// </summary>
/// <remarks>
/// <see cref="SbaCarsRabbitMqFixture"/> itself lives in <c>SbaCars.TestKit</c> (see its own remarks
/// for why). This <c>[CollectionDefinition]</c> stays local on purpose, exactly as
/// <c>SbaCarsPostgresCollection</c> in <c>SbaCars.Persistence.IntegrationTests</c> explains: xUnit
/// resolves a <c>[Collection(name)]</c> attribute against a definition in the *same* test assembly
/// (xunit.analyzers' xUnit1041 enforces this at compile time), so every assembly that wants the
/// collection declares its own four-line registration.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class SbaCarsRabbitMqCollection : ICollectionFixture<SbaCarsRabbitMqFixture>
{
    public const string Name = "SbaCars RabbitMq";
}
