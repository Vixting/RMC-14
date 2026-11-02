using Content.Shared._RMC14.Storage;
using Content.Shared.Item;
using Content.Shared.Storage;

namespace Content.Shared._RMC14.Inventory;

public static class CMInventoryExtensions
{
    public static bool TryGetFirst(EntityUid storageId, EntityUid itemId, out ItemStorageLocation location)
    {
        location = default;

        var entities = IoCManager.Resolve<IEntityManager>();
        var rmcStorageSystem = entities.System<RMCStorageSystem>();

        if (!entities.TryGetComponent(storageId, out StorageComponent? storage) ||
            !entities.TryGetComponent(itemId, out ItemComponent? item))
        {
            return false;
        }

        if (rmcStorageSystem.TryGetReservedLocation((storageId, storage), (itemId, item), out var found) ||
            rmcStorageSystem.TryGetFirstGridLocation((storageId, storage), (itemId, item), out found))
        {
            location = found.Value;
            return true;
        }

        return false;
    }
}
