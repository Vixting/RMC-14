using Content.Shared._RMC14.Projectiles.Penetration;
using Content.Shared._RMC14.Weapons.Ranged.Prediction;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

public sealed class ProjectileImpactParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleBloodColorSystem _bloodColor = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCParticleEffectSystem _particleEffects = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId<ProjectileImpactParticleSettingsComponent> Settings = "RMCProjectileImpactParticleSettings";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, AfterProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<ProjectileComponent> ent, ref AfterProjectileHitEvent args)
    {
        if (HasComp<XenoProjectileComponent>(ent) && !HasComp<ParticleProjectileImpactComponent>(ent))
            return;

        if (_net.IsClient && !IsClientSide(ent))
            return;

        if (GetImpact(ent, args.Target) is not { } impact)
            return;

        ICommonSession? excluded = null;
        if (_net.IsServer && _gun.GunPrediction && TryComp(ent, out PredictedProjectileServerComponent? predicted))
            excluded = predicted.Shooter;

        _particleEffects.PlayImpact(impact, excluded);
    }

    private RMCImpactParticles? GetImpact(Entity<ProjectileComponent> ent, EntityUid target)
    {
        if (GetTravelDirection(ent, target) is not { } direction ||
            !Settings.TryGet(out var settings, _prototype, _compFactory))
        {
            return null;
        }

        var coords = _transform.GetMapCoordinates(ent);
        var intensity = GetIntensity(settings, ent.Comp.Damage.GetTotal());
        var mob = HasComp<MobStateComponent>(target);
        if (HasComp<ParticleProjectileImpactComponent>(ent))
        {
            if (Prototype(ent) is not { } proto)
                return null;

            return new RMCImpactParticles(coords, direction, null, null, 1f, proto.ID, mob);
        }

        if (mob)
        {
            if (!_bloodColor.TryGetHitBloodColor(target, out var blood))
                return null;

            return new RMCImpactParticles(coords, direction, blood, null, intensity);
        }

        ProtoId<ParticleEffectPrototype>? debris = null;
        if (TryComp(target, out ParticleDebrisComponent? impactComp))
            debris = impactComp.Effect;
        else if (TryComp(target, out DamageableComponent? damageable) &&
                 damageable.DamageModifierSetId is { } modifierSet &&
                 settings.MetalModifierSets.Contains(modifierSet))
        {
            debris = settings.MetalDebris;
        }

        return new RMCImpactParticles(coords, direction, null, debris, intensity);
    }

    public bool TryGetIntensity(FixedPoint2 damage, out float intensity)
    {
        if (!Settings.TryGet(out var settings, _prototype, _compFactory))
        {
            intensity = default;
            return false;
        }

        intensity = GetIntensity(settings, damage);
        return true;
    }

    private static float GetIntensity(ProjectileImpactParticleSettingsComponent settings, FixedPoint2 damage)
    {
        return Math.Clamp(MathF.Sqrt(damage.Float() / settings.ReferenceDamage), settings.MinIntensity, settings.MaxIntensity);
    }

    private Angle? GetTravelDirection(Entity<ProjectileComponent> ent, EntityUid target)
    {
        if (TryComp(ent, out PhysicsComponent? physics) && physics.LinearVelocity.LengthSquared() > 0.0001f)
            return physics.LinearVelocity.ToAngle();

        if (ent.Comp.Shooter is not { } shooter || TerminatingOrDeleted(shooter))
            return null;

        var diff = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(shooter);
        return diff.LengthSquared() > 0.0001f ? diff.ToAngle() : null;
    }
}
