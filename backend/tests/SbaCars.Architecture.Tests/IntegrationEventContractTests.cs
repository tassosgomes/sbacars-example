using System.Reflection;
using SbaCars.Contracts;

namespace SbaCars.Architecture.Tests;

/// <summary>
/// B4 gate (§6.4, §9): the fifteen Domain Doc integration events exist as lean <c>record</c>s in
/// <c>SbaCars.Contracts</c>, and any breaking change to wire name, CLR shape, or property type fails
/// the build against the committed <c>schema-snapshot.json</c>.
/// </summary>
public sealed class IntegrationEventContractTests
{
    private static readonly string[] ExpectedDomainDocWireNames =
    [
        "atendimento.atualizado",
        "atendimento.iniciado",
        "catalogo.interesse-solicitado",
        "catalogo.item-atualizado",
        "catalogo.item-publicado",
        "compra.reserva-solicitada",
        "estoque.disponibilidade-alterada",
        "estoque.oferta-atualizada",
        "estoque.oferta-incluida",
        "estoque.oferta-retirada",
        "estoque.reserva-recusada",
        "interesse.manifestado",
        "interesse.qualificado",
        "testdrive.agendado",
        "testdrive.solicitado"
    ];

    private const string FoundationPingWireName = "foundation.ping";

    private static readonly string[] ExpectedWireNames =
    [
        "atendimento.atualizado",
        "atendimento.iniciado",
        "catalogo.interesse-solicitado",
        "catalogo.item-atualizado",
        "catalogo.item-publicado",
        "compra.reserva-solicitada",
        "estoque.disponibilidade-alterada",
        "estoque.oferta-atualizada",
        "estoque.oferta-incluida",
        "estoque.oferta-retirada",
        "estoque.reserva-recusada",
        FoundationPingWireName,
        "interesse.manifestado",
        "interesse.qualificado",
        "testdrive.agendado",
        "testdrive.solicitado"
    ];

    [Fact]
    public void CurrentContracts_MatchCommittedSnapshot()
    {
        var committedJson = File.ReadAllText(RepositoryPaths.ContractsSchemaSnapshotPath);
        var liveJson = ContractSchemaSnapshot.BuildCanonicalJson(typeof(IIntegrationEvent).Assembly);

        ContractSchemaSnapshot.JsonDocumentsAreEquivalent(committedJson, liveJson, out var difference)
            .Should().BeTrue($"live contract schema must match schema-snapshot.json (§9, B4); {difference}");
    }

    [Fact]
    public void FoundationCatalog_ContainsExactlyTheFifteenDocumentedWireNamesPlusFoundationPing()
    {
        var wireNames = ContractSchemaSnapshot
            .CollectEventSchemas(typeof(IIntegrationEvent).Assembly)
            .Select(schema => schema.WireName)
            .ToArray();

        wireNames.Should().Contain(ExpectedDomainDocWireNames, "the fifteen Domain Doc wire names must remain present (B4)");
        wireNames.Should().BeEquivalentTo(ExpectedWireNames, options => options.WithStrictOrdering());
        wireNames.Should().HaveCount(16, "only foundation.ping may be added beyond the fifteen Domain Doc events (B5)");
    }

    [Fact]
    public void EveryFoundationEvent_HasIntegrationEventAttributeMatchingWireNameAndLivesInV1()
    {
        var schemas = ContractSchemaSnapshot.CollectEventSchemas(typeof(IIntegrationEvent).Assembly);

        schemas.Should().AllSatisfy(schema =>
        {
            schema.ClrFullName.Should().Contain(".V1.", "foundation events are versioned by namespace (§6.4)");

            var eventType = typeof(IIntegrationEvent).Assembly.GetType(schema.ClrFullName, throwOnError: true)!;
            eventType.Should().BeAssignableTo<IIntegrationEvent>();

            var attribute = eventType.GetCustomAttribute<IntegrationEventAttribute>();
            attribute.Should().NotBeNull();
            attribute!.Name.Should().Be(schema.WireName);
        });
    }

    [Fact]
    public void FoundationWireNames_AreUnique()
    {
        var wireNames = ContractSchemaSnapshot
            .CollectEventSchemas(typeof(IIntegrationEvent).Assembly)
            .Select(schema => schema.WireName)
            .ToArray();

        wireNames.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void SchemasAreEquivalent_WhenCommittedSnapshotRemovesAProperty_ReportsMismatch()
    {
        var baseline = new[]
        {
            new IntegrationEventSchema(
                "fixture.probe",
                "SbaCars.Architecture.Tests.FixtureProbeIntegrationEvent",
                [
                    new IntegrationEventPropertySchema("EntityId", "System.Guid", false),
                    new IntegrationEventPropertySchema("OcorridoEm", "System.DateTimeOffset", false)
                ])
        };

        var missingProperty = new[]
        {
            new IntegrationEventSchema(
                "fixture.probe",
                "SbaCars.Architecture.Tests.FixtureProbeIntegrationEvent",
                [new IntegrationEventPropertySchema("EntityId", "System.Guid", false)])
        };

        ContractSchemaSnapshot.SchemasAreEquivalent(baseline, missingProperty, out var difference)
            .Should().BeFalse();
        difference.Should().Contain("removed property 'OcorridoEm'");
    }

    [Fact]
    public void SchemasAreEquivalent_WhenCommittedSnapshotChangesPropertyType_ReportsMismatch()
    {
        var baseline = new[]
        {
            new IntegrationEventSchema(
                "fixture.probe",
                "SbaCars.Architecture.Tests.FixtureProbeIntegrationEvent",
                [new IntegrationEventPropertySchema("EntityId", "System.Guid", false)])
        };

        var changedType = new[]
        {
            new IntegrationEventSchema(
                "fixture.probe",
                "SbaCars.Architecture.Tests.FixtureProbeIntegrationEvent",
                [new IntegrationEventPropertySchema("EntityId", "System.String", false)])
        };

        ContractSchemaSnapshot.SchemasAreEquivalent(baseline, changedType, out var difference)
            .Should().BeFalse();
        difference.Should().Contain("type changed");
    }

    [Fact]
    public void SchemasAreEquivalent_WhenCommittedSnapshotChangesWireName_ReportsMismatch()
    {
        var baseline = new[]
        {
            new IntegrationEventSchema(
                "fixture.probe",
                "SbaCars.Architecture.Tests.FixtureProbeIntegrationEvent",
                [new IntegrationEventPropertySchema("EntityId", "System.Guid", false)])
        };

        var renamedWire = new[]
        {
            new IntegrationEventSchema(
                "fixture.probe-renamed",
                "SbaCars.Architecture.Tests.FixtureProbeIntegrationEvent",
                [new IntegrationEventPropertySchema("EntityId", "System.Guid", false)])
        };

        ContractSchemaSnapshot.SchemasAreEquivalent(baseline, renamedWire, out var difference)
            .Should().BeFalse();
        difference.Should().Contain("removed event 'fixture.probe'");
        difference.Should().Contain("added event 'fixture.probe-renamed'");
    }
}
