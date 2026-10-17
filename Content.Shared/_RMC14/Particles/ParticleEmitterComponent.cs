using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class ParticleEmitterComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public List<ProtoId<ParticleEffectPrototype>> Effects = new();

    [DataField, AutoNetworkedField]
    public Color? ColorOverride;

    [DataField, AutoNetworkedField]
    public float Intensity = 1f;

    [DataField, AutoNetworkedField]
    public Vector2 SpawnOffset;
}
