using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedRMCSynthesisSimulatorSystem))]
public sealed partial class RMCSynthesisSimulatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public string TargetSlotId = "synthesis_simulator_target_slot";

    [DataField, AutoNetworkedField]
    public string ReferenceSlotId = "synthesis_simulator_reference_slot";

    [DataField, AutoNetworkedField]
    public SynthesisMode Mode = SynthesisMode.Amplify;

    [DataField, AutoNetworkedField]
    public ProtoId<ChemGeneratorPropertyPrototype>? TargetProperty;

    [DataField, AutoNetworkedField]
    public ProtoId<ChemGeneratorPropertyPrototype>? ReferenceProperty;

    [DataField, AutoNetworkedField]
    public bool Simulating;

    [DataField, AutoNetworkedField]
    public bool Picking;

    [DataField, AutoNetworkedField]
    public bool SimulationFailed;

    [DataField, AutoNetworkedField]
    public List<List<RecipeCandidateIngredient>>? RecipeCandidates;

    [DataField]
    public List<ChemReportProperty>? PendingProperties;

    [DataField]
    public int PendingOverdose;

    [DataField]
    public int PendingTier;

    [DataField]
    public List<string?>? PendingNewIngredientIds;

    [DataField]
    public string? PendingSignature;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan FinishAt;

    [DataField, AutoNetworkedField]
    public TimeSpan ProcessDuration = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? FinishSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");
}

[Serializable, DataDefinition]
public sealed partial class RecipeCandidateIngredient
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField(required: true)]
    public int Amount;

    [DataField]
    public bool Catalyst;
}
