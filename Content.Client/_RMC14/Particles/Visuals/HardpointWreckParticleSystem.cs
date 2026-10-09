using Content.Shared._RMC14.Particles;
using Content.Shared._RMC14.Vehicle;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.Particles;

/// <summary>
/// Keeps smoke emitters attached to damaged or destroyed installed hardpoints until they are repaired or removed.
/// </summary>
public sealed class HardpointWreckParticleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ParticleSystem _particles = default!;

    private readonly Dictionary<EntityUid, (HardpointDamageState State, List<ActiveEmitter> Emitters)> _active = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleOnHardpointDamageComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<ParticleOnHardpointDamageComponent> ent, ref ComponentShutdown args)
    {
        if (_active.Remove(ent, out var active))
            RemoveEmitters(active.Emitters);
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<ParticleOnHardpointDamageComponent, HardpointIntegrityComponent>();
        while (query.MoveNext(out var uid, out var particles, out var integrity))
        {
            var state = GetState(uid, particles, integrity);
            var current = _active.TryGetValue(uid, out var active) ? active.State : HardpointDamageState.Intact;
            if (state == current)
                continue;

            if (current != HardpointDamageState.Intact)
            {
                RemoveEmitters(active.Emitters);
                _active.Remove(uid);
            }

            var effects = state switch
            {
                HardpointDamageState.Damaged => particles.DamagedEffects,
                HardpointDamageState.Destroyed => particles.DestroyedEffects,
                _ => null,
            };

            if (effects == null)
                continue;

            var emitters = new List<ActiveEmitter>();
            foreach (var effect in effects)
            {
                if (_particles.CreateParticle(effect, uid) is { } emitter)
                    emitters.Add(emitter);
            }

            _active[uid] = (state, emitters);
        }
    }

    private HardpointDamageState GetState(EntityUid uid, ParticleOnHardpointDamageComponent particles, HardpointIntegrityComponent integrity)
    {
        if (particles.RequireInstalled && !_container.IsEntityInContainer(uid))
            return HardpointDamageState.Intact;

        if (integrity.Integrity <= 0f)
            return HardpointDamageState.Destroyed;

        if (integrity.MaxIntegrity > 0f && integrity.Integrity <= integrity.MaxIntegrity * particles.DamagedThreshold)
            return HardpointDamageState.Damaged;

        return HardpointDamageState.Intact;
    }

    private void RemoveEmitters(List<ActiveEmitter> emitters)
    {
        foreach (var emitter in emitters)
        {
            _particles.RemoveParticle(emitter);
        }
    }

    private enum HardpointDamageState
    {
        Intact,
        Damaged,
        Destroyed,
    }
}
