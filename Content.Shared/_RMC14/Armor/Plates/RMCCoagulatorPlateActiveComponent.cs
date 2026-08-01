using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor.Plates;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCArmorPlateSystem))]
public sealed partial class RMCCoagulatorPlateActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public int CancelChance = 50;
}
