using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._RMC14.Botany;

[RegisterComponent]
[Access(typeof(RMCLogSystem))]
public sealed partial class RMCLogComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnedPrototype = "MaterialWoodPlank1";

    [DataField]
    public int SpawnCount = 2;
}
