using Content.Client._RMC14.Vehicle;
using Content.Shared._RMC14.Particles;
using Content.Shared._RMC14.Vehicle;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.Particles.Visuals;

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
        SubscribeLocalEvent<ParticleOnHardpointDamageComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ParticleOnHardpointDamageComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ParticleOnHardpointDamageComponent, HardpointIntegrityStateChangedEvent>(OnIntegrityStateChanged);
        SubscribeLocalEvent<ParticleOnHardpointDamageComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ParticleOnHardpointDamageComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnStartup(Entity<ParticleOnHardpointDamageComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent);
    }

    private void OnShutdown(Entity<ParticleOnHardpointDamageComponent> ent, ref ComponentShutdown args)
    {
        if (_active.Remove(ent, out var active))
            RemoveEmitters(active.Emitters);
    }

    private void OnIntegrityStateChanged(Entity<ParticleOnHardpointDamageComponent> ent, ref HardpointIntegrityStateChangedEvent args)
    {
        Refresh(ent);
    }

    private void OnInserted(Entity<ParticleOnHardpointDamageComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        Refresh(ent);
    }

    private void OnRemoved(Entity<ParticleOnHardpointDamageComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        Refresh(ent);
    }

    private void Refresh(Entity<ParticleOnHardpointDamageComponent> ent)
    {
        var state = TryComp(ent, out HardpointIntegrityComponent? integrity)
            ? GetState(ent, ent.Comp, integrity)
            : HardpointDamageState.Intact;

        var current = _active.TryGetValue(ent, out var active) ? active.State : HardpointDamageState.Intact;
        if (state == current)
            return;

        if (current != HardpointDamageState.Intact)
        {
            RemoveEmitters(active.Emitters);
            _active.Remove(ent);
        }

        var effects = state switch
        {
            HardpointDamageState.Damaged => ent.Comp.DamagedEffects,
            HardpointDamageState.Destroyed => ent.Comp.DestroyedEffects,
            _ => null,
        };

        if (effects == null)
            return;

        var emitters = new List<ActiveEmitter>();
        foreach (var effect in effects)
        {
            if (_particles.CreateParticle(effect, ent) is { } emitter)
                emitters.Add(emitter);
        }

        _active[ent] = (state, emitters);
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
