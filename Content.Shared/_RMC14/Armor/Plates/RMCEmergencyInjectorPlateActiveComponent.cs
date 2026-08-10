using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor.Plates;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCArmorPlateSystem))]
public sealed partial class RMCEmergencyInjectorPlateActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Plate;

    [DataField]
    public EntProtoId InjectAction = "RMCActionEmergencyInjectorInject";

    [DataField]
    public EntProtoId ToggleAction = "RMCActionEmergencyInjectorToggleOverdose";

    [DataField, AutoNetworkedField]
    public EntityUid? InjectActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;
}
