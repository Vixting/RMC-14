using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Armor.Plates;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(RMCArmorPlateSystem))]
public sealed partial class RMCEmergencyInjectorPlateActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent = "Epinephrine";

    [DataField, AutoNetworkedField]
    public FixedPoint2 Amount = 15;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(90);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUse;
}
