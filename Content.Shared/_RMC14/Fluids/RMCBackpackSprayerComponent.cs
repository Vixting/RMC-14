using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Fluids;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCBackpackSprayerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId NozzlePrototype = "RMCSprayerNozzle";

    [DataField, AutoNetworkedField]
    public EntityUid? Nozzle;

    [DataField, AutoNetworkedField]
    public string Solution = "tank";
}
