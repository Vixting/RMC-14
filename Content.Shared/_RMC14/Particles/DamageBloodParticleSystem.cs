using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Particles;

public sealed class DamageBloodParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleBloodColorSystem _bloodColor = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly ProjectileImpactParticleSystem _projectileImpact = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<MobStateComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased ||
            args.DamageDelta is not { } delta ||
            args.Origin is not { } origin ||
            origin == ent.Owner ||
            (args.Tool is { } projectile && HasComp<ProjectileComponent>(projectile)) ||
            !_prototype.TryIndex(BruteGroup, out var brute) ||
            !_bloodColor.TryGetHitBloodColor(ent, out var blood))
        {
            return;
        }

        var bruteDamage = FixedPoint2.Zero;
        foreach (var type in brute.DamageTypes)
        {
            if (delta.DamageDict.TryGetValue(type, out var amount) && amount > 0)
                bruteDamage += amount;
        }

        if (bruteDamage <= 0)
            return;

        var predicted = args.Tool is { } tool && HasComp<MeleeWeaponComponent>(tool);
        PlayBlood(ent, origin, bruteDamage, blood, predicted);
    }

    public void PlayBlood(EntityUid target, EntityUid? origin, FixedPoint2 damage, Color blood, bool predicted)
    {
        if (!_projectileImpact.TryGetIntensity(damage, out var intensity))
            return;

        if (_net.IsClient && (!predicted || origin != _player.LocalEntity || !_timing.IsFirstTimePredicted))
            return;

        var coords = _transform.GetMapCoordinates(target);
        var direction = _random.NextAngle();
        if (origin != null)
        {
            var diff = coords.Position - _transform.GetWorldPosition(origin.Value);
            if (diff.LengthSquared() > 0.0001f)
                direction = diff.ToAngle();
        }

        var impact = new ProjectileImpactParticles(coords, direction, blood, null, intensity);
        if (_net.IsClient)
        {
            RaiseLocalEvent(new ProjectileImpactParticlesEvent(new List<ProjectileImpactParticles> { impact }));
            return;
        }

        ICommonSession? excluded = null;
        if (predicted && origin != null)
            _player.TryGetSessionByEntity(origin.Value, out excluded);

        _projectileImpact.QueueImpact(impact, excluded);
    }
}
