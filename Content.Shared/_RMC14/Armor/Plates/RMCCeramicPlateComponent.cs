using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Armor.Plates;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCArmorPlateSystem))]
public sealed partial class RMCCeramicPlateComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Health = 100;

    [DataField, AutoNetworkedField]
    public float MaxHealth = 100;

    [DataField]
    public float FriendlyFireDurabilityMult = 0.3f;

    [DataField]
    public float HostileDurabilityMult = 1f;

    [DataField, AutoNetworkedField]
    public bool Broken;
}

[Serializable, NetSerializable]
public enum RMCCeramicPlateVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum RMCCeramicPlateVisualState : byte
{
    Full,
    Damaged,
    Broken,
}
