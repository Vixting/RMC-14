using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

[RegisterComponent]
public sealed partial class ParticleEmitterComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ParticleEffectPrototype> Effect;

    [DataField]
    public Color? ColorOverride;

    [DataField]
    public float Intensity = 1f;

    [DataField]
    public Vector2 SpawnOffset;
}
