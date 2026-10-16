using System.Numerics;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Deafness;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared.Body.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Explosion;
using Content.Shared.FixedPoint;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Sticky.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Explosion;

public abstract class SharedRMCExplosionSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoAcidSystem _acid = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly RMCDazedSystem _dazed = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedDeafnessSystem _deafness = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly ProtoId<DamageTypePrototype> StructuralDamage = "Structural";
    private static readonly ProtoId<StatusEffectPrototype> FlashedKey = "Flashed";
    private static readonly ProtoId<StatusEffectPrototype> BlindKey = "Blinded";

    private const int ItemDestroyedMessages = 6;

    private readonly HashSet<Entity<RMCWallExplosionDeletableComponent>> _walls = new();
    private readonly List<EntityUid> _heldItems = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<CMExplosionEffectComponent, CMExplosiveTriggeredEvent>(OnExplosionEffectTriggered);

        SubscribeLocalEvent<RMCExplosiveDeleteComponent, CMExplosiveTriggeredEvent>(OnDeleteWallsTriggered);

        SubscribeLocalEvent<ExplosionRandomResistanceComponent, GetExplosionResistanceEvent>(OnExplosionRandomResistanceGet);

        SubscribeLocalEvent<ExplosionProneResistanceComponent, GetExplosionResistanceEvent>(OnExplosionProneResistanceGet);

        SubscribeLocalEvent<StunOnExplosionReceivedComponent, ExplosionReceivedEvent>(OnStunOnExplosionReceivedBeforeExplode);

        SubscribeLocalEvent<DestroyedByExplosionTypeComponent, ExplosionReceivedEvent>(OnDestroyedByExplosionReceived);

        SubscribeLocalEvent<DestroyedByExplosionComponent, ExplosionReceivedEvent>(OnDestroyedByExplosionReceived);

        SubscribeLocalEvent<MobGibbedByExplosionTypeComponent, ExplosionReceivedEvent>(OnMobGibbedByExplosionReceived);
    }

    private void OnExplosionEffectTriggered(Entity<CMExplosionEffectComponent> ent, ref CMExplosiveTriggeredEvent args)
    {
        DoEffect(ent);
    }

    private void OnDeleteWallsTriggered(Entity<RMCExplosiveDeleteComponent> ent, ref CMExplosiveTriggeredEvent args)
    {
        _walls.Clear();
        _entityLookup.GetEntitiesInRange(ent.Owner.ToCoordinates(), ent.Comp.Range, _walls);

        foreach (var wall in _walls)
        {
            QueueDel(wall);
        }

        if (ent.Comp.Whitelist != null &&
            HasComp<StickyComponent>(ent) &&
            Transform(ent).ParentUid is { Valid: true } parent &&
            _entityWhitelist.IsWhitelistPass(ent.Comp.Whitelist, parent))
        {
            QueueDel(parent);
        }
    }

    private void OnExplosionRandomResistanceGet(Entity<ExplosionRandomResistanceComponent> ent, ref GetExplosionResistanceEvent args)
    {
        // this only gets used on the server so randomness isn't an issue for prediction
        var resistance = _random.NextFloat(ent.Comp.Min.Float(), ent.Comp.Max.Float());
        args.DamageCoefficient *= resistance;
    }

    // TODO RMC14 dense mobs absorb blast power in cm13 (20 humanoid/xeno, 40 big)
    private void OnExplosionProneResistanceGet(Entity<ExplosionProneResistanceComponent> ent, ref GetExplosionResistanceEvent args)
    {
        if (args.Epicenter is not { } epicenter ||
            !_standing.IsDown(ent) ||
            IsOnEpicenterTile(ent, epicenter))
        {
            return;
        }

        args.DamageCoefficient *= ent.Comp.Multiplier;
    }

    public void ChangeExplosionStunResistance(EntityUid ent, StunOnExplosionReceivedComponent? comp, bool isStunnable)
    {
        if (!Resolve(ent, ref comp, false))
            return;

        comp.Weak = isStunnable;
    }

    private void OnStunOnExplosionReceivedBeforeExplode(Entity<StunOnExplosionReceivedComponent> ent, ref ExplosionReceivedEvent args)
    {
        var damage = args.Damage.GetTotal();
        var factor = Math.Round(damage.Double() * 0.05) / 2;
        factor = Math.Min(20, factor);

        _sizeStun.TryGetSize(ent, out var size);

        // TODO RMC14 size-based throw ranges and speeds
        var pos = _transform.GetWorldPosition(ent);
        var dir = pos - args.Epicenter.Position;

        // Humanoid calcuations
        if (size == RMCSizes.Humanoid)
        {
            var ev = new CMGetArmorEvent(SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING);
            RaiseLocalEvent(ent, ref ev); // TODO RMC14 limb damage

            var bombArmorMult = (100 - ev.ExplosionArmor) * 0.01;
            var severity = factor * 5;

            if (damage >= ent.Comp.FlashThreshold)
            {
                var flashAttempt = new FlashAttemptEvent(ent, null, null);
                RaiseLocalEvent(ent, ref flashAttempt, true);
                if (!flashAttempt.Cancelled)
                    _statusEffects.TryAddStatusEffect<FlashedComponent>(ent, FlashedKey, ent.Comp.BlindTime * bombArmorMult, true);
            }

            // TODO RMC14 ear damage, cm13 adds severity * 0.15
            _deafness.TryDeafen(ent, TimeSpan.FromSeconds(severity * 0.5), true);

            var knockBackDistance = (float) Math.Clamp(severity / 5 / dir.Length(), 0.5, Math.Max(severity / 10, 0.5));

            if (!HasComp<XenoNestedComponent>(ent))
                _sizeStun.KnockBack(ent, args.Epicenter, knockBackDistance, knockBackDistance, knockBackSpeed: (float) severity);

            var knockdownValue = severity * 0.1;
            var knockoutValue = damage.Double() * 0.1;

            _heldItems.Clear();
            _heldItems.AddRange(_hands.EnumerateHeld(ent.Owner));

            var knockdownMinusArmor = Math.Max(knockdownValue * bombArmorMult, 1);
            var knockdownTime = TimeSpan.FromSeconds(knockdownMinusArmor);
            _stun.TryParalyze(ent, knockdownTime, false);

            foreach (var held in _heldItems)
            {
                ExplosionThrowItem(ent.Comp, held, args.Epicenter, damage.Double());
            }

            var knockoutMinusArmor = Math.Max(knockoutValue * bombArmorMult * 0.5, 0.5);
            var knockoutTime = TimeSpan.FromSeconds(knockoutMinusArmor);
            _sizeStun.TryKnockOut(ent, knockoutTime, false);

            _dazed.TryDaze(ent, knockoutTime * 2, stutter: true);
            _statusEffects.TryAddStatusEffect<RMCBlindedComponent>(ent, BlindKey, ent.Comp.BlurTime, false);
            return;
        }

        if (factor > 0 && ent.Comp.Weak)
        {
            var stunTime = TimeSpan.FromSeconds(factor / 2.5);
            _stun.TryStun(ent, stunTime, true);
            _stun.TryKnockdown(ent, stunTime, true);

            if (size < RMCSizes.Big)
            {
                _slow.TrySlowdown(ent, TimeSpan.FromSeconds(factor));
                _slow.TrySuperSlowdown(ent, TimeSpan.FromSeconds(factor / 2));
            }
            else
                _slow.TrySlowdown(ent, TimeSpan.FromSeconds(factor / 3));

            _sizeStun.KnockBack(ent, args.Epicenter, knockBackSpeed: 5, ignoreSize: true);
        }
        else if (factor > 10)
        {
            factor /= 5;
            var stunTime = TimeSpan.FromSeconds(factor / 5);
            _stun.TryStun(ent, stunTime, true);
            _stun.TryKnockdown(ent, stunTime, true);
            if (size < RMCSizes.Big)
            {
                _slow.TrySlowdown(ent, TimeSpan.FromSeconds(factor));
                _slow.TrySuperSlowdown(ent, TimeSpan.FromSeconds(factor / 2));
            }
            else
                _slow.TrySlowdown(ent, TimeSpan.FromSeconds(factor / 3));
        }
    }

    private void OnDestroyedByExplosionReceived(Entity<DestroyedByExplosionTypeComponent> ent, ref ExplosionReceivedEvent args)
    {
        if (args.Explosion != ent.Comp.Explosion ||
            args.Damage.GetTotal() < ent.Comp.Threshold)
        {
            return;
        }

        if (!TerminatingOrDeleted(ent))
            QueueDel(ent);
    }

    private void OnDestroyedByExplosionReceived(Entity<DestroyedByExplosionComponent> ent, ref ExplosionReceivedEvent args)
    {
        if (!ent.Comp.IsExplodable || TryComp(ent, out CorrodibleComponent? corrodible) && !corrodible.IsCorrodible)
            return;

        if (_container.IsEntityInContainer(ent))
            return;

        var deleteEntity = false;
        var damage = args.Damage.GetTotal();
        var destroyChance =
            damage > ent.Comp.HighIntensityDamageThreshold ? ent.Comp.HighIntensityDestroyChance :
            damage > ent.Comp.LowIntensityDamageThreshold  ? ent.Comp.MediumIntensityDestroyChance :
            ent.Comp.LowIntensityDestroyChance;

        if (_random.NextFloat() < destroyChance)
            deleteEntity = true;


        if (!deleteEntity || TerminatingOrDeleted(ent))
            return;

        if (_acid.TryConsumeGunSecondWind(ent))
            return;

        if (damage <= ent.Comp.LowIntensityDamageThreshold || damage > ent.Comp.HighIntensityDamageThreshold)
        {
            var msg = Loc.GetString($"rmc-explosion-item-destroyed-{_random.Next(1, ItemDestroyedMessages + 1)}", ("item", ent.Owner));
            _popup.PopupCoordinates(msg, Transform(ent).Coordinates, PopupType.MediumCaution);
        }

        QueueDel(ent);
    }

    private void OnMobGibbedByExplosionReceived(Entity<MobGibbedByExplosionTypeComponent> ent, ref ExplosionReceivedEvent args)
    {
        if (Array.IndexOf(ent.Comp.Explosions, args.Explosion) == -1)
            return;

        var total = FixedPoint2.Zero;
        foreach (var (type, amount) in args.Damage.DamageDict)
        {
            if (type == StructuralDamage)
                continue;

            total += amount;
        }

        if (total < ent.Comp.Threshold)
            return;

        if (_net.IsClient)
            return;

        if (!TerminatingOrDeleted(ent))
            _body.GibBody(ent, true);
    }

    private void ExplosionThrowItem(StunOnExplosionReceivedComponent holder, EntityUid item, MapCoordinates epicenter, double severity)
    {
        if (TerminatingOrDeleted(item) ||
            !TryComp(item, out ItemComponent? itemComp) ||
            _container.IsEntityInContainer(item))
        {
            return;
        }

        var xform = Transform(item);
        if (xform.Anchored)
            return;

        var weightClass = Math.Log2(_item.GetSizePrototype(itemComp.Size).Weight) + 1;
        var range = Math.Min(Math.Round(severity / weightClass * holder.HeldThrowRangePerSeverity, MidpointRounding.AwayFromZero), holder.HeldThrowMaxRange);

        var direction = _transform.GetWorldPosition(xform) - epicenter.Position;
        if (IsOnEpicenterTile(item, epicenter) || direction.LengthSquared() == 0)
        {
            direction = _random.NextAngle().ToVec();
            range = Math.Round(range / 2, MidpointRounding.AwayFromZero);
        }

        if (range < 1)
            return;

        var target = Vector2.Normalize(direction) * (float) range;
        if (range >= 2)
        {
            var scatter = range / 4;
            target += new Vector2(
                (float) Math.Round(_random.NextDouble(-scatter, scatter), MidpointRounding.AwayFromZero),
                (float) Math.Round(_random.NextDouble(-scatter, scatter), MidpointRounding.AwayFromZero));
        }

        var speed = (float) Math.Max(range * holder.HeldThrowSpeedPerRange, holder.HeldThrowMinSpeed);
        _throwing.TryThrow(item, target, speed);
    }

    private bool IsOnEpicenterTile(EntityUid ent, MapCoordinates epicenter)
    {
        var coordinates = _transform.GetMapCoordinates(ent);
        if (coordinates.MapId != epicenter.MapId)
            return false;

        if (_mapManager.TryFindGridAt(epicenter, out var gridUid, out var grid))
            return _map.TileIndicesFor(gridUid, grid, coordinates) == _map.TileIndicesFor(gridUid, grid, epicenter);

        return Vector2Helpers.Floored(coordinates.Position) == Vector2Helpers.Floored(epicenter.Position);
    }

    public void DoEffect(Entity<CMExplosionEffectComponent> ent)
    {
        if (ent.Comp.Explosion is { } explosion)
            SpawnNextToOrDrop(explosion, ent);

        if (ent.Comp.MaxShrapnel > 0)
        {
            foreach (var effect in ent.Comp.ShrapnelEffects)
            {
                var shrapnelCount = _random.Next(ent.Comp.MinShrapnel, ent.Comp.MaxShrapnel);
                for (var i = 0; i < shrapnelCount; i++)
                {
                    var angle = _random.NextAngle();
                    var direction = angle.ToVec().Normalized() * 10;
                    var shrapnel = SpawnNextToOrDrop(effect, ent);
                    _throwing.TryThrow(shrapnel, direction, ent.Comp.ShrapnelSpeed / 10);
                }
            }
        }
    }

    public void TryDoEffect(Entity<CMExplosionEffectComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        DoEffect((ent, ent.Comp));
    }

    public virtual void QueueExplosion(
        MapCoordinates epicenter,
        string typeId,
        float totalIntensity,
        float slope,
        float maxTileIntensity,
        EntityUid? cause,
        float tileBreakScale = 1f,
        int maxTileBreak = int.MaxValue,
        bool canCreateVacuum = true,
        bool addLog = true)
    {
    }

    public virtual void TriggerExplosive(
        EntityUid uid,
        bool delete = true,
        float? totalIntensity = null,
        float? radius = null,
        EntityUid? user = null)
    {
    }
}
