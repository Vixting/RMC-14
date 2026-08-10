using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Biomass;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedRMCBiomassAnalyzerSystem))]
public sealed partial class RMCBiomassAnalyzerMachineComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? PrintingUntil;

    [DataField, AutoNetworkedField]
    public EntityUid? HeldOrgan;

    [DataField, AutoNetworkedField]
    public int HeldOrganValue;

    [DataField, AutoNetworkedField]
    public bool AutoProcessOrgan;

    [DataField, AutoNetworkedField]
    public List<string> PrintQueue = new();

    [DataField, AutoNetworkedField]
    public bool QueueProcessing;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextQueueItemAt;
}
