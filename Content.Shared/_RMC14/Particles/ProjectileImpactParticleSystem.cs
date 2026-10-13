using Content.Shared._RMC14.Projectiles.Penetration;
using Content.Shared._RMC14.Weapons.Ranged.Prediction;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Damage;
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
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId<ProjectileImpactParticleSettingsComponent> Settings = "RMCProjectileImpactParticleSettings";

    private readonly List<(ProjectileImpactParticles Impact, ICommonSession? Excluded)> _pending = new();
    private readonly Dictionary<ICommonSession, List<ProjectileImpactParticles>> _bySession = new();

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

        if (_net.IsClient)
        {
            RaiseLocalEvent(new ProjectileImpactParticlesEvent(new List<ProjectileImpactParticles> { impact }));
            return;
        }

        ICommonSession? excluded = null;
        if (_gun.GunPrediction && TryComp(ent, out PredictedProjectileServerComponent? predicted))
            excluded = predicted.Shooter;

        _pending.Add((impact, excluded));
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient || _pending.Count == 0)
            return;

        _bySession.Clear();
        foreach (var (impact, excluded) in _pending)
        {
            foreach (var session in Filter.Pvs(impact.Coords).Recipients)
            {
                if (session == excluded)
                    continue;

                if (!_bySession.TryGetValue(session, out var impacts))
                {
                    impacts = new List<ProjectileImpactParticles>();
                    _bySession[session] = impacts;
                }

                impacts.Add(impact);
            }
        }

        _pending.Clear();
        foreach (var (session, impacts) in _bySession)
        {
            RaiseNetworkEvent(new ProjectileImpactParticlesEvent(impacts), session);
        }

        _bySession.Clear();
    }

    private ProjectileImpactParticles? GetImpact(Entity<ProjectileComponent> ent, EntityUid target)
    {
        if (GetTravelDirection(ent, target) is not { } direction ||
            !Settings.TryGet(out var settings, _prototype, _compFactory))
        {
            return null;
        }

        var coords = _transform.GetMapCoordinates(ent);
        var intensity = Math.Clamp(MathF.Sqrt(ent.Comp.Damage.GetTotal().Float() / settings.ReferenceDamage), settings.MinIntensity, settings.MaxIntensity);
        var mob = HasComp<MobStateComponent>(target);
        if (HasComp<ParticleProjectileImpactComponent>(ent))
        {
            if (Prototype(ent) is not { } proto)
                return null;

            return new ProjectileImpactParticles(coords, direction, null, null, 1f, proto.ID, mob);
        }

        if (mob)
        {
            if (!_bloodColor.TryGetBloodColor(target, out var blood))
                return null;

            return new ProjectileImpactParticles(coords, direction, blood, null, intensity);
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

        return new ProjectileImpactParticles(coords, direction, null, debris, intensity);
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
