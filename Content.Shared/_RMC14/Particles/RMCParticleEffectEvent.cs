using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Particles;

[Serializable, NetSerializable]
public sealed class RMCParticleEffectEvent : EntityEventArgs
{
    public List<ProtoId<ParticleEffectPrototype>> Effects;
    public MapCoordinates Coords;
    public Angle? Direction;

    public Color? Color;

    public TimeSpan? Duration;
    public float? Radius;

    public bool TileTint;

    public float Brightness = 1f;
    public int? Count;
    public Angle? Spread;
    public float? Intensity;

    public NetEntity? Source;

    public RMCParticleEffectEvent(
        List<ProtoId<ParticleEffectPrototype>> effects,
        MapCoordinates coords,
        Angle? direction = null,
        Color? color = null,
        TimeSpan? duration = null,
        float? radius = null)
    {
        Effects = effects;
        Coords = coords;
        Direction = direction;
        Color = color;
        Duration = duration;
        Radius = radius;
    }
}

[Serializable, NetSerializable]
public sealed class RMCParticleEffectStopEvent : EntityEventArgs
{
    public NetEntity Source;

    public RMCParticleEffectStopEvent(NetEntity source)
    {
        Source = source;
    }
}

[Serializable, NetSerializable]
public sealed class RMCParticleEffectBatchEvent : EntityEventArgs
{
    public List<NetEntity> Stops;
    public List<RMCParticleEffectEvent> Effects;

    public RMCParticleEffectBatchEvent(List<NetEntity> stops, List<RMCParticleEffectEvent> effects)
    {
        Stops = stops;
        Effects = effects;
    }
}
