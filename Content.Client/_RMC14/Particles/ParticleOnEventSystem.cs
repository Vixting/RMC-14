using Content.Client._RMC14.Weapons.Ranged;
using Content.Shared._RMC14.Particles;
using Content.Shared.Throwing;
using Content.Shared.Trigger;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Particles;

public sealed class ParticleOnEventSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, List<ActiveEmitter>> _thrownEmitters = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThrownItemComponent, ComponentStartup>(OnThrownStartup);
        SubscribeLocalEvent<ThrownItemComponent, ComponentRemove>(OnThrownRemove);
        SubscribeLocalEvent<ParticleOnThrownComponent, ComponentShutdown>(OnThrownShutdown);
        SubscribeLocalEvent<ParticleOnMuzzleFlashComponent, RMCMuzzleFlashCreatedEvent>(OnMuzzleFlash);
    }

    private void OnThrownStartup(Entity<ThrownItemComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out ParticleOnThrownComponent? particles))
            return;

        if (particles.RequirePrimed &&
            (!_appearance.TryGetData(ent, TriggerVisuals.VisualState, out TriggerVisualState state) ||
             state != TriggerVisualState.Primed))
        {
            return;
        }

        StopThrownEmitter(ent.Owner);

        var emitters = new List<ActiveEmitter>();
        foreach (var effect in particles.Effects)
        {
            StartThrownEmitter(ent, particles, effect, emitters);
        }

        if (emitters.Count > 0)
            _thrownEmitters[ent.Owner] = emitters;
    }

    private void StartThrownEmitter(EntityUid uid, ParticleOnThrownComponent particles, ProtoId<ParticleEffectPrototype> effect, List<ActiveEmitter> emitters)
    {
        if (_particles.CreateParticle(effect, uid, particles.ColorOverride) is { } emitter)
            emitters.Add(emitter);
    }

    private void OnThrownRemove(Entity<ThrownItemComponent> ent, ref ComponentRemove args)
        => StopThrownEmitter(ent.Owner);

    private void OnThrownShutdown(Entity<ParticleOnThrownComponent> ent, ref ComponentShutdown args)
        => StopThrownEmitter(ent.Owner);

    private void StopThrownEmitter(EntityUid uid)
    {
        if (!_thrownEmitters.Remove(uid, out var emitters))
            return;

        foreach (var emitter in emitters)
        {
            _particles.RemoveParticle(emitter);
        }
    }

    private void OnMuzzleFlash(Entity<ParticleOnMuzzleFlashComponent> ent, ref RMCMuzzleFlashCreatedEvent args)
    {
        var coords = _transform.GetMapCoordinates(args.Flash);
        var direction = ent.Comp.Reverse ? args.Angle + Math.PI : args.Angle;
        foreach (var effect in ent.Comp.Effects)
        {
            _particles.CreateParticle(effect, coords, direction, ent.Comp.ColorOverride);
        }
    }
}
