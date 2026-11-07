using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Particles;

/// <param name="Direction">Direction hit travelled in</param>
/// <param name="Blood">Set when mob hit.</param>
/// <param name="Debris">Material debris thrown off target.</param>
/// <param name="Intensity">Scales particle count & size, from the hits damage.</param>
/// <param name="Projectile">Set when the projectile has <see cref="ParticleProjectileImpactComponent"/>, replacing default.</param>
/// <param name="HitMob">Whether a mob was hit.</param>
[Serializable, NetSerializable]
public readonly record struct RMCImpactParticles(
    MapCoordinates Coords,
    Angle Direction,
    Color? Blood,
    ProtoId<ParticleEffectPrototype>? Debris,
    float Intensity,
    EntProtoId? Projectile = null,
    bool HitMob = false);
