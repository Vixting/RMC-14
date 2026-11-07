using Content.Client._RMC14.Vehicle;
using Content.Shared._RMC14.Particles;
using Content.Shared._RMC14.Vehicle;

namespace Content.Client._RMC14.Particles.Visuals;

public sealed class VehicleDamageParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;

    private readonly Dictionary<EntityUid, ActiveSmoke> _active = new();

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(ParticleSystem));
        SubscribeLocalEvent<ParticleOnVehicleDamageComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ParticleOnVehicleDamageComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ParticleOnVehicleDamageComponent, HardpointIntegrityStateChangedEvent>(OnIntegrityStateChanged);
        SubscribeLocalEvent<RMCParticlesResetEvent>(OnParticlesReset);
    }

    private void OnParticlesReset(ref RMCParticlesResetEvent args)
    {
        _active.Clear();
        var query = EntityQueryEnumerator<ParticleOnVehicleDamageComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            Refresh((uid, comp), false);
        }
    }

    private void OnStartup(Entity<ParticleOnVehicleDamageComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent, false);
    }

    private void OnShutdown(Entity<ParticleOnVehicleDamageComponent> ent, ref ComponentShutdown args)
    {
        if (_active.Remove(ent, out var active))
            RemoveEmitters(active.Emitters);
    }

    private void OnIntegrityStateChanged(Entity<ParticleOnVehicleDamageComponent> ent, ref HardpointIntegrityStateChangedEvent args)
    {
        Refresh(ent, true);
    }

    private void Refresh(Entity<ParticleOnVehicleDamageComponent> ent, bool fromHit)
    {
        var integrityComp = CompOrNull<HardpointIntegrityComponent>(ent);
        var integrity = integrityComp?.Integrity;
        var state = GetState(ent.Comp, integrityComp);
        _active.TryGetValue(ent, out var active);

        var flare = fromHit && (active == null || integrity < active.Integrity);
        if (active != null && active.State == state)
        {
            if (flare)
                active.Elapsed = 0;

            active.Integrity = integrity ?? 0;
            return;
        }

        if (active != null)
        {
            RemoveEmitters(active.Emitters);
            _active.Remove(ent);
        }

        var effects = state switch
        {
            VehicleDamageState.Damaged => ent.Comp.DamagedEffects,
            VehicleDamageState.Destroyed => ent.Comp.DestroyedEffects,
            _ => null,
        };

        if (effects == null)
            return;

        active = new ActiveSmoke(ent.Comp, state)
        {
            Integrity = integrity ?? 0,
            Elapsed = flare ? 0 : (float) ent.Comp.FlareDuration.TotalSeconds,
        };

        foreach (var effect in effects)
        {
            if (_particles.CreateParticle(effect, ent) is { } emitter)
                active.Emitters.Add(emitter);
        }

        _active[ent] = active;
        ApplyFlare(active);
    }

    public override void FrameUpdate(float frameTime)
    {
        foreach (var active in _active.Values)
        {
            active.Elapsed += frameTime;
            ApplyFlare(active);
        }
    }

    private static void ApplyFlare(ActiveSmoke active)
    {
        var duration = (float) active.Settings.FlareDuration.TotalSeconds;
        var t = duration > 0 ? Math.Clamp(active.Elapsed / duration, 0f, 1f) : 1f;
        var wisp = active.Settings.WispFraction;
        var factor = wisp + (1 - wisp) * (1 - t) * (1 - t);

        foreach (var emitter in active.Emitters)
        {
            emitter.Overrides ??= new ParticleRuntimeOverrides();
            emitter.Overrides.EmissionRate = emitter.Proto.EmissionRate * factor;
        }
    }

    private static VehicleDamageState GetState(ParticleOnVehicleDamageComponent particles, HardpointIntegrityComponent? integrity)
    {
        if (integrity == null)
            return VehicleDamageState.Intact;

        if (integrity.Integrity <= 0f)
            return VehicleDamageState.Destroyed;

        if (integrity.MaxIntegrity > 0f && integrity.Integrity <= integrity.MaxIntegrity * particles.DamagedThreshold)
            return VehicleDamageState.Damaged;

        return VehicleDamageState.Intact;
    }

    private void RemoveEmitters(List<ActiveEmitter> emitters)
    {
        foreach (var emitter in emitters)
        {
            _particles.RemoveParticle(emitter);
        }
    }

    private enum VehicleDamageState
    {
        Intact,
        Damaged,
        Destroyed,
    }

    private sealed class ActiveSmoke(ParticleOnVehicleDamageComponent settings, VehicleDamageState state)
    {
        public readonly ParticleOnVehicleDamageComponent Settings = settings;
        public readonly VehicleDamageState State = state;
        public readonly List<ActiveEmitter> Emitters = new();
        public float Integrity;
        public float Elapsed;
    }
}
