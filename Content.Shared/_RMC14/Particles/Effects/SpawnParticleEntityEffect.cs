using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles.Effects;

/// <summary>
/// Entity effect that spawns a particle effect on the target entity (client-side only).
/// Use this in any EntityEffect[] list instead of dedicated ParticleOn* components.
/// </summary>
public sealed partial class SpawnParticleEffect : EventEntityEffect<SpawnParticleEffect>
{
    [DataField(required: true)]
    public ProtoId<ParticleEffectPrototype> Effect;

    [DataField]
    public Color? ColorOverride;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}
