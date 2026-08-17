using System.Diagnostics;

namespace SbaCars.Inventory.Application.Common;

/// <summary>Activity source for inventory application use cases.</summary>
public static class InventoryActivitySource
{
    public const string Name = "SbaCars.Inventory";

    public static readonly ActivitySource Instance = new(Name);
}
