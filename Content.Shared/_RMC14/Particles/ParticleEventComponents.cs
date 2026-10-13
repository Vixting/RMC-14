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

/// <summary>
/// Spawns a particle effect at the muzzle each time this gun shows a muzzle flash.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleOnMuzzleFlashComponent : ParticleOnEventBase
{
    [DataField]
    public bool Reverse;
}

/// <summary>
/// Debris thrown back toward the shooter when a projectile hits this entity.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleDebrisComponent : Component
{
    [DataField]
    public ProtoId<ParticleEffectPrototype>? Effect;

    [DataField]
    public int DestructionCount = 24;

    [DataField]
    public float DestructionIntensity = 1.4f;
}

/// <summary>
/// Replaces the defalt impact, blood & debris particles when this projectile hits something.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleProjectileImpactComponent : Component
{
    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> Effects = new();

    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> SurfaceEffects = new();

    [DataField]
    public Color? Color;
}

/// <summary>
/// Plays the tiny tier of an explosion particle set where this entity spawns.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleExplosionOnSpawnComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<ExplosionParticlesComponent> Particles;

    [DataField]
    public float Probability = 1f;
}

[RegisterComponent]
public sealed partial class ParticleOnHardpointDamageComponent : Component
{
    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> DamagedEffects = new();

    [DataField]
    public float DamagedThreshold = 0.5f;

    [DataField]
    public bool RequireInstalled = true;

    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> DestroyedEffects = new();
}
