using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

[DataDefinition]
public sealed partial class ExplosionParticleTier
{
    [DataField]
    public float MinPower;

    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> Effects = new();

    /// <summary>
    /// Played instead of <see cref="Effects"/> when epicenter is water.
    /// </summary>
    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> WaterEffects = new();

    /// <summary>
    /// Played instead of <see cref="Effects"/> when epicenter tile matches one of the surfaces.
    /// </summary>
    [DataField]
    public List<ExplosionParticleSurface> Surfaces = new();

    /// <summary>
    /// Played alongside <see cref="Effects"/>, tinted with the avrg color of the epicenter tile.
    /// </summary>
    [DataField]
    public List<ExplosionParticleTileColorEffect> TileColorEffects = new();

    [DataField]
    public EntProtoId? Flash;
}
