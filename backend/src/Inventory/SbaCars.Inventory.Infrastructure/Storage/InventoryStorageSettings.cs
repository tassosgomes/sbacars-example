using Microsoft.Extensions.Options;
using SbaCars.BuildingBlocks.Storage;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Infrastructure.Storage;

internal sealed class InventoryStorageSettings(IOptions<StorageOptions> options) : IInventoryStorageSettings
{
    public string BucketName => options.Value.BucketName;
}
