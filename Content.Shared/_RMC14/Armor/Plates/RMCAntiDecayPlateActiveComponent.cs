using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor.Plates;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCArmorPlateSystem))]
public sealed partial class RMCAntiDecayPlateActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DamageMultiplier = 0.7f;
}
