using Content.Shared._RMC14.Particles;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Particles;

public sealed class GibMistParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;

    private static readonly ProtoId<ParticleEffectPrototype> MistEffect = "RMCGibMist";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GibMistParticleEvent>(OnGibMist);
    }

    private void OnGibMist(GibMistParticleEvent ev)
    {
        var emitter = _particles.SpawnEffect(MistEffect, ev.Coords);
        if (emitter == null)
            return;

        emitter.ColorOverride = ev.BloodColor;
    }
}
