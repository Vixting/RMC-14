using Content.Shared._RMC14.Particles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Client._RMC14.Particles;

public sealed class ParticleOnEventSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;

    private readonly Dictionary<EntityUid, ActiveEmitter> _thrownEmitters = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleOnThrownComponent, ThrownEvent>(OnThrown);
        SubscribeLocalEvent<ParticleOnThrownComponent, LandEvent>(OnThrownLanded);
        SubscribeLocalEvent<ParticleOnThrownComponent, ComponentShutdown>(OnThrownShutdown);
        SubscribeLocalEvent<ParticleOnGunShotProjectileComponent, AmmoShotEvent>(OnGunShotProjectile);
    }

    private void OnThrown(Entity<ParticleOnThrownComponent> ent, ref ThrownEvent args)
    {
        StopThrownEmitter(ent.Owner);
        var emitter = _particles.CreateParticle(ent.Comp.Effect, ent.Owner, ent.Comp.ColorOverride);
        if (emitter != null)
            _thrownEmitters[ent.Owner] = emitter;
    }

    private void OnThrownLanded(Entity<ParticleOnThrownComponent> ent, ref LandEvent args)
        => StopThrownEmitter(ent.Owner);

    private void OnThrownShutdown(Entity<ParticleOnThrownComponent> ent, ref ComponentShutdown args)
        => StopThrownEmitter(ent.Owner);

    private void StopThrownEmitter(EntityUid uid)
    {
        if (_thrownEmitters.Remove(uid, out var emitter))
            _particles.RemoveParticle(emitter);
    }

    private void OnGunShotProjectile(Entity<ParticleOnGunShotProjectileComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
            _particles.CreateParticle(ent.Comp.Effect, projectile, ent.Comp.ColorOverride);
    }
}
