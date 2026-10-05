using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

public abstract partial class ParticleOnEventBase : Component
{
    [DataField(required: true)]
    public ProtoId<ParticleEffectPrototype> Effect;

    [DataField]
    public Color? ColorOverride;
}

/// <summary>
/// Spawns a particle effect while this entity is in flight after being thrown.
/// Stopped automatically on landing or deletion.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleOnThrownComponent : ParticleOnEventBase
{
}

/// <summary>
/// Spawns a particle effect on each projectile fired by this gun.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleOnGunShotProjectileComponent : ParticleOnEventBase
{
}
