using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Spawns particle while this entity is in flight after being thrown.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class ParticleOnThrownComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public List<ProtoId<ParticleEffectPrototype>> Effects = new();

    [DataField, AutoNetworkedField]
    public Color? ColorOverride;

    [DataField, AutoNetworkedField]
    public bool RequirePrimed;
}
