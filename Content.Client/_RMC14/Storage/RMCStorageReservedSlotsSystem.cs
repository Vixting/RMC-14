using Content.Client.Storage;
using Content.Shared._RMC14.Storage;
using Content.Shared.Storage;
using Robust.Shared.GameStates;

namespace Content.Client._RMC14.Storage;

public sealed class RMCStorageReservedSlotsSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCStorageReservedSlotsComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAfterHandleState(Entity<RMCStorageReservedSlotsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (_ui.TryGetOpenUi<StorageBoundUserInterface>(ent.Owner, StorageComponent.StorageUiKey.Key, out var bui))
                bui.Refresh();
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(StorageBoundUserInterface)}\n{e}");
        }
    }
}
