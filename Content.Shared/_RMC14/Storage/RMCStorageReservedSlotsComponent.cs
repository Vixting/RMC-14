using Content.Shared.Storage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(RMCStorageSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class RMCStorageReservedSlotsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, ItemStorageLocation> Reserved = new();

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> HardReserved = new();
}
