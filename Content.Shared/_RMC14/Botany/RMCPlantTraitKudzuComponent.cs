using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPlantTraitKudzuComponent : RMCPlantTraitComponent
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), AutoNetworkedField]
    public string KudzuPrototype = "WeakKudzu";

    [DataField, AutoNetworkedField]
    public float WeedHighLevelThreshold = 10f;

    public override string? TraitState { get; set; } = "mutation-plant-kudzu";
}
