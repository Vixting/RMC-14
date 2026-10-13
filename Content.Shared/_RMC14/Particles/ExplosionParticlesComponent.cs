using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Particle effects played at an explosions epicenter, picked by epicenter power.
/// </summary>
[RegisterComponent]
public sealed partial class ExplosionParticlesComponent : Component
{
    /// <summary>
    /// Used for explosions flagged as tiny & for anything below the weakest tier.
    /// </summary>
    [DataField(required: true)]
    public ExplosionParticleTier Tiny = new();

    [DataField]
    public List<ExplosionParticleTier> Tiers = new();
}

[DataDefinition]
public sealed partial class ExplosionParticleTier
{
    [DataField]
    public float MinPower;

    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> Effects = new();

    /// <summary>
    /// Played instead of <see cref="Effects"/> when the epicenter is on water.
    /// </summary>
    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> WaterEffects = new();

    /// <summary>
    /// Played instead of <see cref="Effects"/> when the epicenter tile matches one of the surfaces.
    /// </summary>
    [DataField]
    public List<ExplosionParticleSurface> Surfaces = new();

    /// <summary>
    /// Played alongside <see cref="Effects"/>, tinted with the avrg color of the epicenter tile.
    /// Skipped on water or a matching surface.
    /// </summary>
    [DataField]
    public List<ExplosionParticleTileColorEffect> TileColorEffects = new();

    /// <summary>
    /// Client entity spawned at a random rotation when the epicenter is not on water.
    /// </summary>
    [DataField]
    public EntProtoId? Flash;
}

[DataDefinition]
public sealed partial class ExplosionParticleTileColorEffect
{
    [DataField(required: true)]
    public ProtoId<ParticleEffectPrototype> Effect;

    [DataField]
    public float Brightness = 1f;

    /// <summary>
    /// Used when the tile has no texture to sample.
    /// </summary>
    [DataField]
    public Color Fallback = Color.White;
}

[DataDefinition]
public sealed partial class ExplosionParticleSurface
{
    [DataField(required: true)]
    public List<ProtoId<ContentTileDefinition>> Tiles = new();

    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> Effects = new();
}
