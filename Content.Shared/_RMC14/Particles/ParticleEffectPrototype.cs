using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Particles;

[DataDefinition]
public sealed partial class ParticleCurveKey
{
    [DataField(required: true)] public float Time { get; private set; }
    [DataField(required: true)] public float Value { get; private set; }
}

[DataDefinition]
public sealed partial class ColorCurveKey
{
    [DataField(required: true)] public float Time { get; private set; }
    [DataField(required: true)] public Color Color { get; private set; }
}

[DataDefinition]
public sealed partial class Vector2CurveKey
{
    [DataField(required: true)] public float Time { get; private set; }
    [DataField(required: true)] public Vector2 Value { get; private set; }
}

[DataDefinition]
public sealed partial class ParticleBurstData
{
    [DataField] public TimeSpan Time { get; private set; }
    [DataField] public int Count { get; private set; } = 10;
}

public enum EmissionShapeType : byte
{
    Point,
    CircleEdge,
    CircleFill,
    Box,
}

[DataDefinition]
public sealed partial class EmissionShapeData
{
    [DataField] public EmissionShapeType Type { get; private set; } = EmissionShapeType.Point;
    [DataField] public float Radius { get; private set; } = 0.5f;
    [DataField] public Vector2 BoxExtents { get; private set; } = new(0.5f, 0.5f);
}

[Prototype]
public sealed partial class ParticleEffectPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ParticleEffectPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField(required: true)] public SpriteSpecifier Sprite { get; private set; } = default!;
    [DataField] public Color StartColor { get; private set; } = Color.White;
    [DataField] public Color EndColor { get; private set; } = Color.Transparent;
    [DataField] public List<ColorCurveKey> ColorOverLifetime { get; private set; } = new();
    [DataField] public List<ParticleCurveKey> AlphaOverLifetime { get; private set; } = new();
    [DataField] public string? Shader { get; private set; }
    [DataField] public int RenderLayer { get; private set; }

    /// <summary>
    /// When true, always renders at full quality regardless of user settings.
    /// Use ONLY for gameplay-critical particles, never cosmetic effects.
    /// </summary>
    [DataField] public bool IgnoreQualitySettings { get; private set; }

    [DataField] public float ParticleSize { get; private set; } = 0.2f;
    [DataField] public float SizeVariance { get; private set; }
    [DataField] public List<ParticleCurveKey> SizeOverLifetime { get; private set; } = new();
    [DataField] public float StretchFactor { get; private set; }

    [DataField] public TimeSpan Lifetime { get; private set; } = TimeSpan.FromSeconds(1);
    [DataField] public TimeSpan LifetimeVariance { get; private set; } = TimeSpan.FromSeconds(0.2);

    [DataField] public float Speed { get; private set; } = 1f;
    [DataField] public float SpeedVariance { get; private set; } = 0.3f;
    [DataField] public List<ParticleCurveKey> SpeedOverLifetime { get; private set; } = new();
    [DataField] public Vector2 ConstantForce { get; private set; }
    [DataField] public List<Vector2CurveKey> ForceOverLifetime { get; private set; } = new();
    [DataField] public List<Vector2CurveKey> VelocityOverLifetime { get; private set; } = new();
    [DataField] public float Gravity { get; private set; }
    [DataField] public float Drag { get; private set; }
    [DataField] public float TerminalSpeed { get; private set; }
    [DataField] public float NoiseStrength { get; private set; }
    [DataField] public float NoiseFrequency { get; private set; } = 1f;
    [DataField] public float InheritVelocity { get; private set; }

    [DataField] public Angle StartRotation { get; private set; }
    [DataField] public Angle StartRotationVariance { get; private set; }
    [DataField] public Angle RotationSpeed { get; private set; }
    [DataField] public Angle RotationSpeedVariance { get; private set; }
    [DataField] public bool AlignToVelocity { get; private set; }

    [DataField] public float EmissionRate { get; private set; } = 20f;
    [DataField] public List<ParticleCurveKey> EmissionOverTime { get; private set; } = new();
    [DataField] public int MaxCount { get; private set; } = 50;
    [DataField] public bool Burst { get; private set; }
    [DataField] public List<ParticleBurstData> Bursts { get; private set; } = new();
    [DataField] public TimeSpan Duration { get; private set; }

    [DataField] public bool WorldSpace { get; private set; } = true;
    [DataField] public Vector2 SpawnOffset { get; private set; }

    [DataField] public EmissionShapeData Shape { get; private set; } = new();

    [DataField] public Angle SpreadAngle { get; private set; } = Angle.FromDegrees(360);
    [DataField] public Angle EmitAngle { get; private set; }

    [DataField] public ProtoId<ParticleEffectPrototype>? SubEmitterOnDeath { get; private set; }
    [DataField] public ProtoId<ParticleEffectPrototype>? SubEmitterOnSpawn { get; private set; }
}
