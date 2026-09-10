using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._RMC14.Botany;

public enum HarvestType : byte
{
    NoRepeat,
    Repeat,
    SelfHarvest,
}

/// <summary>
/// Harvestable products & yield of a <see cref="RMCPlantComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class RMCPlantHarvestComponent : Component
{
    /// <summary>
    /// Rntity prototype this seed spawns when it gets harvested.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>)), AutoNetworkedField]
    public List<string> ProductPrototypes = new();

    /// <summary>
    /// Entity prototype that is spawned when this plant is sampled/extracted into a seed packet.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), AutoNetworkedField]
    public string PacketPrototype = "SeedBase";

    [DataField, AutoNetworkedField]
    public int Yield;

    [DataField, AutoNetworkedField]
    public HarvestType HarvestRepeat = HarvestType.NoRepeat;

    [DataField, AutoNetworkedField]
    public bool Seedless;

    [DataField, AutoNetworkedField]
    public float RepeatHarvestCounter;
}
