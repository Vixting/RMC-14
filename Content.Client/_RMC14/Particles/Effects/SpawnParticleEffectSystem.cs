using Content.Shared._RMC14.Particles.Effects;
using Content.Shared.EntityEffects;

namespace Content.Client._RMC14.Particles.Effects;

/// <summary>
/// Handles <see cref="SpawnParticleEffect"/> on the client, spawning a particle effect on the target entity.
/// </summary>
public sealed class SpawnParticleEffectSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExecuteEntityEffectEvent<SpawnParticleEffect>>(OnExecute);
    }

    private void OnExecute(ref ExecuteEntityEffectEvent<SpawnParticleEffect> args)
    {
        _particles.CreateParticle(args.Effect.Effect, args.Args.TargetEntity, args.Effect.ColorOverride);
    }
}
