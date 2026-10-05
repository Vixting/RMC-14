using System.Numerics;

namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Per-emitter runtime overrides for <see cref="ParticleEffectPrototype"/> fields.
/// Null = use prototype value.
/// </summary>
public sealed class ParticleRuntimeOverrides
{
    public Color? StartColor;
    public Color? EndColor;
    public Color? ColorOverride;
    public string? Shader;
    public int? RenderLayer;

    public float? ParticleSize;
    public float? SizeVariance;
    public float? StretchFactor;

    public TimeSpan? Lifetime;
    public TimeSpan? LifetimeVariance;

    public float? Speed;
    public float? SpeedVariance;
    public Vector2? ConstantForce;
    public float? Gravity;
    public float? Drag;
    public float? TerminalSpeed;
    public float? NoiseStrength;
    public float? NoiseFrequency;
    public float? InheritVelocity;

    public Angle? StartRotation;
    public Angle? StartRotationVariance;
    public Angle? RotationSpeed;
    public Angle? RotationSpeedVariance;

    public float? EmissionRate;
    public int? MaxCount;
    public TimeSpan? Duration;
    public Angle? SpreadAngle;
    public Angle? EmitAngle;

    public Vector2? SpawnOffset;
}
