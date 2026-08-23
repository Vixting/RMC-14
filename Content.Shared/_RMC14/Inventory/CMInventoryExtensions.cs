using Content.Shared._RMC14.Storage;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared._RMC14.Inventory;

public static class CMInventoryExtensions
{
    public static bool TryGetFirst(EntityUid storageId, EntityUid itemId, out ItemStorageLocation location)
    {
        location = default;

        var entities = IoCManager.Resolve<IEntityManager>();
        var storageSystem = entities.System<SharedStorageSystem>();
        var rmcStorageSystem = entities.System<RMCStorageSystem>();

        if (!entities.TryGetComponent(storageId, out StorageComponent? storage) ||
            !entities.TryGetComponent(itemId, out ItemComponent? item))
        {
            return false;
        }

        if (rmcStorageSystem.TryGetReservedLocation((storageId, storage), (itemId, item), out var reservedLocation))
        {
            location = reservedLocation.Value;
            return true;
        }

        var storageBounding = storage.Grid.GetBoundingBox();

        ItemStorageLocation? reservedFallback = null;

        for (var y = storageBounding.Bottom; y <= storageBounding.Top; y++)
        {
            for (var x = storageBounding.Left; x <= storageBounding.Right; x++)
            {
                location = new ItemStorageLocation(0, (x, y));
                if (storageSystem.ItemFitsInGridLocation(itemId, storageId, location))
                {
                    if (rmcStorageSystem.IsHardReservedForOther((storageId, storage), (itemId, item), location))
                        continue;

                    if (rmcStorageSystem.IsReservedForOther((storageId, storage), (itemId, item), location))
                    {
                        reservedFallback ??= location;
                        continue;
                    }

                    return true;
                }
            }
        }

        if (reservedFallback is { } fallback)
        {
            location = fallback;
            return true;
        }

        location = default;
        return false;
    }
}
