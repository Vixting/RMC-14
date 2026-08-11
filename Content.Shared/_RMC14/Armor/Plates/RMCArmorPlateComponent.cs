using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor.Plates;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCArmorPlateComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public RMCArmorPlateKind Kind;

    [DataField, AutoNetworkedField]
    public int Magnitude = 1;
}

public enum RMCArmorPlateKind
{
    Coagulator,
    EmergencyInjector,
    Ceramic,
    AntiDecay,
    Translator,
}
