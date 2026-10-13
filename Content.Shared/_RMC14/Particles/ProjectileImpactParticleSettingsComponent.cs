using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

[RegisterComponent]
public sealed partial class ProjectileImpactParticleSettingsComponent : Component
{
    [DataField]
    public float ReferenceDamage = 40;

    [DataField]
    public float MinIntensity = 0.5f;

    [DataField]
    public float MaxIntensity = 1.6f;

    [DataField]
    public ProtoId<ParticleEffectPrototype>? MetalDebris;

    [DataField]
    public HashSet<ProtoId<DamageModifierSetPrototype>> MetalModifierSets = new();
}
