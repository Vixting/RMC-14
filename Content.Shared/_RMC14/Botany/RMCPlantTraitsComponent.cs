using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class RMCPlantTraitsComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ResPath PlantRsi;

    [DataField, AutoNetworkedField]
    public string PlantIconState = "produce";

    [DataField, AutoNetworkedField]
    public float PestTolerance = 5f;

    [DataField, AutoNetworkedField]
    public float WeedTolerance = 5f;

    [DataField, AutoNetworkedField]
    public float ToxinsTolerance = 4f;

    [DataField, AutoNetworkedField]
    public string? SplatPrototype;
}
