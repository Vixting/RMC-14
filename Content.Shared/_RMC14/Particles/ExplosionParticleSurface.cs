using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

[DataDefinition]
public sealed partial class ExplosionParticleSurface
{
    [DataField(required: true)]
    public List<ProtoId<ContentTileDefinition>> Tiles = new();

    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> Effects = new();
}
