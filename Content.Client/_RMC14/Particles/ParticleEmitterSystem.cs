using Content.Shared._RMC14.Particles;

namespace Content.Client._RMC14.Particles;

/// <summary>
/// Spawns a particle effect when an entity with <see cref="ParticleEmitterComponent"/> initializes (including PVS re-entry).
/// </summary>
public sealed class ParticleEmitterSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, ActiveEmitter> _activeEmitters = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleEmitterComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ParticleEmitterComponent, ComponentShutdown>(OnCompShutdown);
    }

    private void OnCompInit(Entity<ParticleEmitterComponent> ent, ref ComponentInit args)
    {
        if (_activeEmitters.TryGetValue(ent.Owner, out var old))
        {
            _particles.RemoveParticle(old);
            _activeEmitters.Remove(ent.Owner);
        }

        var coords = _transform.GetMapCoordinates(ent.Owner);
        var emitter = _particles.SpawnEffect(ent.Comp.Effect, coords, ent.Owner, ent.Comp.ColorOverride);
        if (emitter == null)
            return;

        if (ent.Comp.Intensity != 1f)
            emitter.Intensity = ent.Comp.Intensity;

        if (ent.Comp.SpawnOffset != default)
            emitter.SpawnOffset = ent.Comp.SpawnOffset;

        _activeEmitters[ent.Owner] = emitter;
    }

    private void OnCompShutdown(Entity<ParticleEmitterComponent> ent, ref ComponentShutdown args)
    {
        if (_activeEmitters.Remove(ent.Owner, out var emitter))
            _particles.RemoveParticle(emitter);
    }
}
