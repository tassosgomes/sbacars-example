using SbaCars.TestKit;

namespace SbaCars.Inventory.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class InventoryPostgresCollection : ICollectionFixture<SbaCarsPostgresFixture>
{
    public const string Name = "inventory-postgres";
}
