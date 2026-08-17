using SbaCars.TestKit;

namespace SbaCars.Inventory.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class InventoryMinioCollection : ICollectionFixture<SbaCarsMinioFixture>
{
    public const string Name = "inventory-minio";
}
