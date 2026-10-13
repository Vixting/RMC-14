using Content.Shared._RMC14.Particles;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent]
public sealed partial class RMCExplosionParticlesOverrideComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<ExplosionParticlesComponent> Particles;
}
