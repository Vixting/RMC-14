using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

/// <summary>
/// Maturation/lifespan/production timing for a <see cref="RMCPlantComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class RMCPlantGrowthComponent : Component
{
    /// <summary>
    /// Max health of the plant.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Endurance = 100f;

    [DataField, AutoNetworkedField]
    public float Lifespan;

    [DataField, AutoNetworkedField]
    public float Maturation;

    [DataField, AutoNetworkedField]
    public float Production;

    [DataField, AutoNetworkedField]
    public int GrowthStages = 6;

    /// <summary>
    /// If false, rapidly decrease health while growing
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Viable = true;

    [DataField, AutoNetworkedField]
    public int LastProduce;
}
