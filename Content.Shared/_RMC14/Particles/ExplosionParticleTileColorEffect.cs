using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

[DataDefinition]
public sealed partial class ExplosionParticleTileColorEffect
{
    [DataField(required: true)]
    public ProtoId<ParticleEffectPrototype> Effect;

    [DataField]
    public float Brightness = 1f;

    [DataField]
    public Color Fallback = Color.White;
}
