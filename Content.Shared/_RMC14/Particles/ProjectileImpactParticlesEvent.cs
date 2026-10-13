using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Particles;

[Serializable, NetSerializable]
public sealed class ProjectileImpactParticlesEvent : EntityEventArgs
{
    public List<ProjectileImpactParticles> Impacts;

    public ProjectileImpactParticlesEvent(List<ProjectileImpactParticles> impacts)
    {
        Impacts = impacts;
    }
}

/// <param name="Direction">The projectiles direction of travel.</param>
/// <param name="Blood">Set when a mob was hit.</param>
/// <param name="Debris">Material debris thrown off a target.</param>
/// <param name="Intensity">Scales particle count & size, from the projectiles damage.</param>
/// <param name="Projectile">Set when the projectile has <see cref="ParticleProjectileImpactComponent"/>, replacing default.</param>
/// <param name="HitMob">Whether a mob was hit.</param>
[Serializable, NetSerializable]
public readonly record struct ProjectileImpactParticles(
    MapCoordinates Coords,
    Angle Direction,
    Color? Blood,
    ProtoId<ParticleEffectPrototype>? Debris,
    float Intensity,
    EntProtoId? Projectile = null,
    bool HitMob = false);
