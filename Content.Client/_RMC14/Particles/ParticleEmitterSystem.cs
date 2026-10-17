using Content.Shared._RMC14.Particles;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Particles;

/// <summary>
/// Spawns a particle effect when an entity with <see cref="ParticleEmitterComponent"/> initializes (including PVS re-entry).
/// </summary>
public sealed class ParticleEmitterSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, List<ActiveEmitter>> _activeEmitters = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleEmitterComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ParticleEmitterComponent, ComponentShutdown>(OnCompShutdown);
    }

    private void OnCompInit(Entity<ParticleEmitterComponent> ent, ref ComponentInit args)
    {
        RemoveEmitters(ent.Owner);

        var emitters = new List<ActiveEmitter>();
        foreach (var effect in ent.Comp.Effects)
        {
            SpawnEmitter(ent, effect, emitters);
        }

        if (emitters.Count > 0)
            _activeEmitters[ent.Owner] = emitters;
    }

    private void SpawnEmitter(Entity<ParticleEmitterComponent> ent, ProtoId<ParticleEffectPrototype> effect, List<ActiveEmitter> emitters)
    {
        var coords = _transform.GetMapCoordinates(ent.Owner);
        var emitter = _particles.SpawnEffect(effect, coords, ent.Owner, ent.Comp.ColorOverride);
        if (emitter == null)
            return;

        if (ent.Comp.Intensity != 1f)
            emitter.Intensity = ent.Comp.Intensity;

        if (ent.Comp.SpawnOffset != default)
            emitter.SpawnOffset = ent.Comp.SpawnOffset;

        emitters.Add(emitter);
    }

    public IReadOnlyList<ActiveEmitter> GetEmitters(EntityUid uid)
    {
        return _activeEmitters.TryGetValue(uid, out var emitters) ? emitters : Array.Empty<ActiveEmitter>();
    }

    private void OnCompShutdown(Entity<ParticleEmitterComponent> ent, ref ComponentShutdown args)
    {
        RemoveEmitters(ent.Owner);
    }

    private void RemoveEmitters(EntityUid uid)
    {
        if (!_activeEmitters.Remove(uid, out var emitters))
            return;

        foreach (var emitter in emitters)
        {
            _particles.RemoveParticle(emitter);
        }
    }
}
