namespace SbaCars.BuildingBlocks.Storage;

/// <summary>
/// Canonical S3 bucket names for the SBACars object store (§7). Compose bootstrap (C2) creates
/// these buckets; C3 wires <see cref="StorageOptions"/> and presigned URL endpoints per service.
/// </summary>
public static class ObjectStorageBuckets
{
    /// <summary>Catalog photos and media (D01).</summary>
    public const string CatalogMedia = "sbacars-catalog-media";

    /// <summary>Inventory documents and evidence (D02).</summary>
    public const string InventoryDocs = "sbacars-inventory-docs";

    /// <summary>Purchase dossier — sensitive personal data (D04); separate bucket from day one.</summary>
    public const string PurchaseDossier = "sbacars-purchase-dossier";
}
