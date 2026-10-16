using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

public sealed class StructureDebrisParticleSystem : EntitySystem
{
    [Dependency] private readonly RMCParticleEffectSystem _particleEffects = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ParticleEffectPrototype> HitSmoke = "RMCImpactSmoke";
    private static readonly ProtoId<ParticleEffectPrototype> DestructionDust = "RMCDestructionDust";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleDebrisComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ParticleDebrisComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnDamageChanged(Entity<ParticleDebrisComponent> ent, ref DamageChangedEvent args)
    {
        if (ent.Comp.Effect is not { } debris ||
            !args.DamageIncreased ||
            args.Origin is not { } origin ||
            origin == ent.Owner ||
            (args.Tool is { } tool && HasComp<ProjectileComponent>(tool)))
        {
            return;
        }

        var coords = _transform.GetMapCoordinates(ent);
        var diff = _transform.GetWorldPosition(origin) - coords.Position;
        Angle? direction = diff.LengthSquared() > 0.0001f ? diff.ToAngle() : null;
        var ev = new RMCParticleEffectEvent(new List<ProtoId<ParticleEffectPrototype>> { debris, HitSmoke }, coords, direction);
        if (args.Tool is { } melee && HasComp<MeleeWeaponComponent>(melee))
            _particleEffects.PlayPredicted(ev, origin);
        else
            _particleEffects.PlayPvs(ev);
    }

    private void OnDestruction(Entity<ParticleDebrisComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.Effect is not { } debris)
            return;

        var coords = _transform.GetMapCoordinates(ent);
        _particleEffects.PlayPvs(new RMCParticleEffectEvent(new List<ProtoId<ParticleEffectPrototype>> { debris }, coords)
        {
            Count = ent.Comp.DestructionCount,
            Spread = Angle.FromDegrees(360),
            Intensity = ent.Comp.DestructionIntensity,
        });

        _particleEffects.PlayPvs(new RMCParticleEffectEvent(new List<ProtoId<ParticleEffectPrototype>> { DestructionDust }, coords));
    }
}
