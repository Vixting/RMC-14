using Content.Shared._RMC14.Particles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client._RMC14.Particles;

public sealed partial class ParticleSystem
{
    /// <summary>
    /// Spawns a particle effect at the entity's position, optionally attaching to follow it.
    /// </summary>
    public ActiveEmitter? CreateParticle(
        ProtoId<ParticleEffectPrototype> effectId,
        EntityUid entity,
        Color? colorOverride = null,
        bool attach = true,
        ParticleRuntimeOverrides? overrides = null,
        Vector2? initialVelocity = null)
    {
        var coords = _transform.GetMapCoordinates(entity);
        return SpawnEffect(effectId, coords, attach ? entity : null, colorOverride, overrides, initialVelocity);
    }

    /// <summary>
    /// Spawns a particle effect at the given map coordinates.
    /// </summary>
    public ActiveEmitter? CreateParticle(
        ProtoId<ParticleEffectPrototype> effectId,
        MapCoordinates coords,
        Color? colorOverride = null,
        ParticleRuntimeOverrides? overrides = null,
        Vector2? initialVelocity = null)
    {
        return SpawnEffect(effectId, coords, null, colorOverride, overrides, initialVelocity);
    }

    /// <summary>
    /// Spawns a particle effect at the given map coordinates, emitting along a world space direction
    /// </summary>
    public ActiveEmitter? CreateParticle(
        ProtoId<ParticleEffectPrototype> effectId,
        MapCoordinates coords,
        Angle worldDirection,
        Color? colorOverride = null,
        ParticleRuntimeOverrides? overrides = null)
    {
        overrides ??= new ParticleRuntimeOverrides();
        overrides.EmitAngle = WorldToEmitAngle(worldDirection);
        return SpawnEffect(effectId, coords, null, colorOverride, overrides);
    }

    /// <summary>
    /// Converts a world space direction into the screen space emit angle used by emitters
    /// </summary>
    public Angle WorldToEmitAngle(Angle worldDirection)
    {
        var world = worldDirection.ToVec();
        var eyeAngle = (float) _eye.CurrentEye.Rotation;
        var cos = MathF.Cos(eyeAngle);
        var sin = MathF.Sin(eyeAngle);
        var screen = new Vector2(world.X * cos - world.Y * sin, world.X * sin + world.Y * cos);
        return new Angle(MathF.Atan2(screen.X, screen.Y));
    }

    /// <summary>
    /// Converts a world space direction into the particle rotation that points a sprites top along it
    /// </summary>
    public Angle WorldToParticleRotation(Angle worldDirection)
    {
        var world = worldDirection.ToVec();
        var eyeAngle = (float) _eye.CurrentEye.Rotation;
        return new Angle(MathF.Atan2(-world.X, world.Y) + eyeAngle);
    }

    /// <summary>Stops and removes a particle emitter by reference.</summary>
    public void RemoveParticle(ActiveEmitter? emitter)
    {
        if (emitter != null)
            StopEffect(emitter);
    }

    /// <summary>Stops and removes a particle emitter by handle.</summary>
    public void RemoveParticle(uint handle)
    {
        StopEffect(handle);
    }
}
