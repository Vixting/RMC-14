using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared._RMC14.WeedKiller;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Jittering;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Evolution;

public sealed class XenoEvolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly SharedXenoHiveSystem _xenoHive = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;
    [Dependency] private readonly IMapManager _map = default!;

    private readonly HashSet<EntityUid> _hivesWithRaffleRan = new();

    private TimeSpan _evolutionPointsRequireOvipositorAfter;
    private TimeSpan _evolutionAccumulatePointsBefore;
    private TimeSpan _evolveSameCasteCooldown;
    private TimeSpan _earlyEvoBoostBefore;

    private readonly HashSet<EntityUid> _climbable = new();
    private readonly HashSet<EntityUid> _doors = new();
    private readonly HashSet<EntityUid> _intersecting = new();

    private EntityQuery<MobStateComponent> _mobStateQuery;

    public override void Initialize()
    {
        _mobStateQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<XenoDevolveComponent, XenoOpenDevolveActionEvent>(OnXenoOpenDevolveAction);

        SubscribeLocalEvent<XenoEvolutionComponent, MapInitEvent>(OnXenoEvolveMapInit);
        SubscribeLocalEvent<XenoEvolutionComponent, XenoOpenEvolutionsActionEvent>(OnXenoEvolveAction);
        SubscribeLocalEvent<XenoEvolutionComponent, XenoEvolutionDoAfterEvent>(OnXenoEvolveDoAfter);
        SubscribeLocalEvent<XenoEvolutionComponent, NewXenoEvolvedEvent>(OnXenoEvolutionNewEvolved);
        SubscribeLocalEvent<XenoEvolutionComponent, XenoDevolvedEvent>(OnXenoEvolutionDevolved);

        SubscribeLocalEvent<XenoNewlyEvolvedComponent, PreventCollideEvent>(OnNewlyEvolvedPreventCollide);

        SubscribeLocalEvent<XenoEvolutionGranterComponent, NewXenoEvolvedEvent>(OnGranterEvolved);

        SubscribeLocalEvent<XenoOvipositorChangedEvent>(OnOvipositorChanged);

        Subs.BuiEvents<XenoEvolutionComponent>(XenoEvolutionUIKey.Key,
            subs =>
            {
                subs.Event<XenoEvolveBuiMsg>(OnXenoEvolveBui);
                subs.Event<XenoStrainBuiMsg>(OnXenoStrainBui);
                subs.Event<XenoT3RaffleEntryMsg>(OnXenoT3RaffleEntryBui);
            });

        Subs.BuiEvents<XenoDevolveComponent>(XenoDevolveUIKey.Key,
            subs =>
            {
                subs.Event<XenoDevolveBuiMsg>(OnXenoDevolveBui);
            });

        Subs.CVar(_config, RMCCVars.RMCEvolutionPointsRequireOvipositorMinutes, v => _evolutionPointsRequireOvipositorAfter = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCEvolutionPointsAccumulateBeforeMinutes, v => _evolutionAccumulatePointsBefore = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCXenoEvolveSameCasteCooldownSeconds, v => _evolveSameCasteCooldown = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCXenoEarlyEvoPointBoostBeforeMinutes, v => _earlyEvoBoostBefore = TimeSpan.FromMinutes(v), true);
    }

    private void OnXenoOpenDevolveAction(Entity<XenoDevolveComponent> xeno, ref XenoOpenDevolveActionEvent args)
    {
        if (args.Handled)
            return;

        if (!DamagedCheckPopup(xeno))
            return;

        args.Handled = true;
        _ui.OpenUi(xeno.Owner, XenoDevolveUIKey.Key, xeno);
    }

    private void OnXenoEvolveMapInit(Entity<XenoEvolutionComponent> ent, ref MapInitEvent args)
    {
        _action.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);
    }

    private void OnXenoEvolveAction(Entity<XenoEvolutionComponent> xeno, ref XenoOpenEvolutionsActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _ui.OpenUi(xeno.Owner, XenoEvolutionUIKey.Key, xeno);

        _ui.SetUiState(xeno.Owner, XenoEvolutionUIKey.Key, GetBuiState(xeno));
    }

    private XenoEvolveBuiState GetBuiState(Entity<XenoEvolutionComponent> xeno)
    {
        var hasQueen = false;
        if (_xenoHive.GetHive(xeno.Owner) is { } hive)
            hasQueen = _xenoHive.HasHiveQueen(hive);

        return new XenoEvolveBuiState(
            LackingOvipositor(),
            IsWeedKillerDeployed(),
            hasQueen,
            IsT3Unlocked()
        );
    }

    public bool IsWeedKillerDeployed()
    {
        var query = EntityQueryEnumerator<WeedKillerComponent>();
        while (query.MoveNext(out _, out var wk))
        {
            if (wk.Deployed)
                return true;
        }

        return false;
    }

    public bool IsT3Unlocked()
    {
        return _gameTicker.RoundDuration() >= TimeSpan.FromSeconds(900);
    }

    private void OnXenoEvolveBui(Entity<XenoEvolutionComponent> xeno, ref XenoEvolveBuiMsg args)
    {
        var actor = args.Actor;
        _ui.CloseUi(xeno.Owner, XenoEvolutionUIKey.Key, actor);

        if (_net.IsClient)
            return;

        if (!CanEvolvePopup(xeno, args.Choice))
        {
            Log.Warning($"{ToPrettyString(actor)} sent an invalid evolution choice: {args.Choice}.");
            return;
        }

        if (!DamagedCheckPopup(xeno, false))
            return;

        var time = _timing.CurTime;
        if (_prototypes.TryIndex(args.Choice, out var choice) &&
            choice.HasComponent<XenoEvolutionGranterComponent>(_compFactory) &&
            _xenoHive.GetHive(xeno.Owner) is { } hive &&
            hive.Comp.LastQueenDeath is { } lastQueenDeath &&
            time < lastQueenDeath + hive.Comp.NewQueenCooldown)
        {
            var left = lastQueenDeath + hive.Comp.NewQueenCooldown - time;
            var msg = Loc.GetString("rmc-xeno-evolution-cant-evolve-recent-queen-death-minutes",
                ("minutes", left.Minutes),
                ("seconds", left.Seconds));
            if (left.Minutes == 0)
            {
                msg = Loc.GetString("rmc-xeno-evolution-cant-evolve-recent-queen-death-seconds",
                    ("seconds", left.Seconds));
            }

            _popup.PopupEntity(msg, xeno, xeno, PopupType.MediumCaution);
            return;
        }

        var ev = new XenoEvolutionDoAfterEvent(args.Choice);
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.EvolutionDelay, ev, xeno)
        {
            BreakOnRest = false,
        };

        if (xeno.Comp.EvolutionDelay > TimeSpan.Zero)
            _popup.PopupClient(Loc.GetString("cm-xeno-evolution-start"), xeno, xeno);

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _jitter.DoJitter(xeno, xeno.Comp.EvolutionDelay, true, 80, 8, true);

            var popupOthers = Loc.GetString("rmc-xeno-evolution-start-others", ("xeno", xeno));
            _popup.PopupEntity(popupOthers, xeno, Filter.PvsExcept(xeno), true, PopupType.Medium);

            var popupSelf = Loc.GetString("rmc-xeno-evolution-start-self");
            _popup.PopupEntity(popupSelf, xeno, xeno, PopupType.Medium);
        }
    }

    private void OnXenoStrainBui(Entity<XenoEvolutionComponent> xeno, ref XenoStrainBuiMsg args)
    {
        var actor = args.Actor;
        _ui.CloseUi(xeno.Owner, XenoEvolutionUIKey.Key, actor);

        if (_net.IsClient)
            return;

        if (!xeno.Comp.Strains.Contains(args.Choice))
        {
            Log.Warning($"{ToPrettyString(actor)} sent an invalid strain choice: {args.Choice}.");
            return;
        }

        if (!ContainedCheckPopup(xeno))
            return;

        if (!DamagedCheckPopup(xeno, false))
            return;

        var newXeno = TransferXeno(xeno, args.Choice);
        var ev = new NewXenoEvolvedEvent(xeno, newXeno, false);
        RaiseLocalEvent(newXeno, ref ev, true);

        _adminLog.Add(LogType.RMCEvolve, $"Xenonid {ToPrettyString(xeno)} chose strain {ToPrettyString(newXeno)}");

        Del(xeno.Owner);

        var afterEv = new AfterNewXenoEvolvedEvent();
        RaiseLocalEvent(newXeno, ref afterEv);
    }

    private void OnXenoT3RaffleEntryBui(Entity<XenoEvolutionComponent> xeno, ref XenoT3RaffleEntryMsg args)
    {
        if (_net.IsClient)
            return;

        if (IsT3Unlocked())
            return;

        if (args.Choice == null)
        {
            xeno.Comp.T3RaffleChoice = null;
            Dirty(xeno);
            return;
        }

        var choice = args.Choice.Value;
        if (!xeno.Comp.EvolvesTo.Contains(choice))
            return;

        if (!_prototypes.TryIndex(choice, out var proto))
            return;

        if (!proto.TryGetComponent(out XenoComponent? choiceXeno, _compFactory) || choiceXeno.Tier < 3)
            return;

        xeno.Comp.T3RaffleChoice = choice;
        Dirty(xeno);

        _popup.PopupEntity(
            Loc.GetString("rmc-xeno-t3-raffle-registered", ("choice", proto.Name)),
            xeno, xeno, PopupType.Medium
        );
    }

    private void OnXenoDevolveBui(Entity<XenoDevolveComponent> xeno, ref XenoDevolveBuiMsg args)
    {
        _ui.CloseUi(xeno.Owner, XenoEvolutionUIKey.Key, xeno);
        TryDevolve(xeno, args.Choice);
    }

    private void OnXenoEvolveDoAfter(Entity<XenoEvolutionComponent> xeno, ref XenoEvolutionDoAfterEvent args)
    {
        if (_net.IsClient ||
            args.Handled ||
            args.Cancelled ||
            !_mind.TryGetMind(xeno, out _, out _) ||
            !CanEvolvePopup(xeno, args.Choice))
        {
            return;
        }

        args.Handled = true;

        var newXeno = TransferXeno(xeno, args.Choice);
        var ev = new NewXenoEvolvedEvent(xeno, newXeno, true);
        RaiseLocalEvent(newXeno, ref ev, true);

        _adminLog.Add(LogType.RMCEvolve, $"Xenonid {ToPrettyString(xeno)} evolved into {ToPrettyString(newXeno)}");

        Del(xeno.Owner);

        _popup.PopupEntity(Loc.GetString("cm-xeno-evolution-end"), newXeno, newXeno);

        var afterEv = new AfterNewXenoEvolvedEvent();
        RaiseLocalEvent(newXeno, ref afterEv);
    }

    private void OnXenoEvolutionNewEvolved(Entity<XenoEvolutionComponent> xeno, ref NewXenoEvolvedEvent args)
    {
        TransferPoints((args.OldXeno, args.OldXeno), xeno, args.SubtractPoints);
        _jitter.DoJitter(xeno, xeno.Comp.EvolutionJitterDuration, true, 80, 8, true);

        xeno.Comp.T3RaffleChoice = args.OldXeno.Comp.T3RaffleChoice;
        Dirty(xeno);
    }

    private void OnXenoEvolutionDevolved(Entity<XenoEvolutionComponent> xeno, ref XenoDevolvedEvent args)
    {
        TransferPoints(args.OldXeno, (xeno, xeno), false);

        if (_net.IsServer &&
            !IsT3Unlocked() &&
            args.OldXeno.Comp.T3RaffleChoice != null)
        {
            _popup.PopupEntity(
                Loc.GetString("rmc-xeno-t3-raffle-forfeit"),
                xeno, xeno, PopupType.MediumCaution
            );
        }
    }

    private void TransferPoints(Entity<XenoEvolutionComponent?> old, Entity<XenoEvolutionComponent> xeno, bool subtract)
    {
        if (!Resolve(old, ref old.Comp, false))
            return;

        xeno.Comp.Points = subtract ? FixedPoint2.Max(0, old.Comp.Points - old.Comp.Max) : old.Comp.Points;

        Dirty(xeno);
    }

    private void OnNewlyEvolvedPreventCollide(Entity<XenoNewlyEvolvedComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.StopCollide.Contains(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnGranterEvolved(Entity<XenoEvolutionGranterComponent> ent, ref NewXenoEvolvedEvent args)
    {
        _xenoAnnounce.AnnounceSameHive(ent.Owner, Loc.GetString("rmc-new-queen"));
    }

    private void OnOvipositorChanged(ref XenoOvipositorChangedEvent ev)
    {
        if (_net.IsClient)
            return;

        var xenos = EntityQueryEnumerator<ActorComponent, XenoEvolutionComponent>();
        while (xenos.MoveNext(out var uid, out _, out var evoComp))
        {
            _ui.SetUiState(uid, XenoEvolutionUIKey.Key, GetBuiState((uid, evoComp)));
        }
    }

    private bool ContainedCheckPopup(EntityUid xeno, bool doPopup = true)
    {
        if (!_container.IsEntityInContainer(xeno))
            return true;

        if (doPopup)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-evolution-failed-bad-location"), xeno, xeno, PopupType.MediumCaution);

        return false;
    }

    private bool DamagedCheckPopup(EntityUid xeno, bool predicted = true, bool doPopup = true)
    {
        if (!TryComp(xeno, out DamageableComponent? damageable) ||
            damageable.TotalDamage <= 1)
            return true;

        if (predicted)
            _popup.PopupClient(Loc.GetString("rmc-xeno-evolution-cant-evolve-damaged"), xeno, xeno, PopupType.MediumCaution);
        else
            _popup.PopupEntity(Loc.GetString("rmc-xeno-evolution-cant-evolve-damaged"), xeno, xeno, PopupType.MediumCaution);

        return false;
    }

    private bool CanEvolvePopup(Entity<XenoEvolutionComponent> xeno, EntProtoId newXeno, bool doPopup = true)
    {
        var isEarlyEvolve = xeno.Comp.EarlyEvolvesTo.Contains(newXeno) && !IsWeedKillerDeployed();
        if (!xeno.Comp.EvolvesTo.Contains(newXeno) &&
            !xeno.Comp.EvolvesToWithoutPoints.Contains(newXeno) &&
            !isEarlyEvolve)
            return false;

        if (!_prototypes.TryIndex(newXeno, out var prototype))
            return true;

        if (!ContainedCheckPopup(xeno, doPopup))
            return false;

        // TODO RMC14 revive jelly when added should not bring back dead queens
        if (prototype.TryGetComponent(out XenoEvolutionCappedComponent? capped, _compFactory) &&
            HasLiving<XenoEvolutionCappedComponent>(capped.Max, e => e.Comp.Id == capped.Id))
        {
            if (doPopup)
                _popup.PopupEntity(Loc.GetString("cm-xeno-evolution-failed-already-have", ("prototype", prototype.Name)), xeno, xeno, PopupType.MediumCaution);

            return false;
        }

        if (prototype.HasComponent<XenoEvolutionGranterComponent>(_compFactory) &&
            HasLiving<XenoEvolutionGranterComponent>(1))
        {
            if (doPopup)
            {
                _popup.PopupEntity(
                    Loc.GetString("rmc-xeno-evolution-queen-already-exists"),
                    xeno,
                    xeno,
                    PopupType.MediumCaution
                );
            }

            return false;
        }

        if (!xeno.Comp.CanEvolveWithoutGranter && !HasLiving<XenoEvolutionGranterComponent>(1))
        {
            if (doPopup)
            {
                _popup.PopupEntity(
                    Loc.GetString("cm-xeno-evolution-failed-hive-shaken"),
                    xeno,
                    xeno,
                    PopupType.MediumCaution
                );
            }

            return false;
        }


        if (TryComp<RestrictEvolveOffWeedsComponent>(xeno.Owner, out var comp))
        {
            var coordinates = _transform.GetMoverCoordinates(xeno).SnapToGrid(EntityManager, _map);
            if (_transform.GetGrid(coordinates) is not { } gridUid ||
                !TryComp(gridUid, out MapGridComponent? grid))
            {
                return false;
            }

            if (!_xenoWeeds.IsOnWeeds((gridUid, grid), coordinates) && comp.RestrictTime > _gameTicker.RoundDuration())
            {
                _popup.PopupEntity(
                    Loc.GetString("rmc-xeno-evolution-failed-early-weeds"),
                    xeno,
                    xeno,
                    PopupType.MediumCaution
                );
                return false;
            }
        }

        prototype.TryGetComponent(out XenoComponent? newXenoComp, _compFactory);
        if (newXenoComp != null &&
            newXenoComp.UnlockAt > _gameTicker.RoundDuration())
        {
            if (doPopup)
            {
                _popup.PopupEntity(
                    Loc.GetString("cm-xeno-evolution-failed-cannot-support"),
                    xeno,
                    xeno,
                    PopupType.MediumCaution
                );
            }

            return false;
        }

        if (!_net.IsClient &&
            newXenoComp != null &&
            newXenoComp.Tier >= 3 &&
            IsT3Unlocked() &&
            _xenoHive.GetHive(xeno.Owner) is { } t3Hive &&
            !_hivesWithRaffleRan.Contains(t3Hive.Owner))
        {
            if (doPopup)
            {
                _popup.PopupEntity(
                    Loc.GetString("rmc-xeno-evolution-t3-raffle-pending"),
                    xeno,
                    xeno,
                    PopupType.MediumCaution
                );
            }

            return false;
        }

        if (newXenoComp != null &&
            !newXenoComp.BypassTierCount &&
            _xenoHive.GetHive(xeno.Owner) is { } oldHive &&
            _xenoHive.TryGetTierLimit((oldHive, oldHive.Comp), newXenoComp.Tier, out var limit))
        {
            var existing = 0;
            var total = Math.Sqrt(oldHive.Comp.BurrowedLarva * oldHive.Comp.BurrowedLarvaSlotFactor);
            total = Math.Min(total, oldHive.Comp.BurrowedLarva);

            var current = EntityQueryEnumerator<XenoComponent, HiveMemberComponent>();
            var slotCount = oldHive.Comp.FreeSlots.ToDictionary();
            while (current.MoveNext(out var uid, out var existingComp, out var member))
            {
                if (_mobState.IsDead(uid))
                    continue;

                if (member.Hive != oldHive.Owner || !existingComp.CountedInSlots)
                    continue;

                total++;

                if (existingComp.Tier < newXenoComp.Tier)
                    continue;

                if (slotCount.ContainsKey(existingComp.Role.Id) && slotCount[existingComp.Role.Id] > 0)
                    slotCount[existingComp.Role.Id] -= 1;
                else
                    existing++;
            }

            if (total != 0 && existing / (float) total >= limit && (!slotCount.ContainsKey(newXeno) || slotCount[newXeno] <= 0))
            {
                if (doPopup)
                {
                    _popup.PopupEntity(
                        Loc.GetString("cm-xeno-evolution-failed-hive-full", ("tier", newXenoComp.Tier)),
                        xeno,
                        xeno,
                        PopupType.MediumCaution
                    );
                }

                return false;
            }
        }

        if (TryComp(xeno, out XenoRecentlyDevolvedComponent? recently) &&
            recently.Recent.TryGetValue(newXeno, out var at) &&
            at + _evolveSameCasteCooldown > _timing.CurTime)
        {
            var timeRemaining = at + _evolveSameCasteCooldown - _timing.CurTime;
            var msg = Loc.GetString("rmc-xeno-evolution-cant-evolve-caste-cooldown",
                ("minutes", timeRemaining.Minutes),
                ("seconds", timeRemaining.Seconds));

            if (doPopup)
                _popup.PopupEntity(msg, xeno, xeno, PopupType.MediumCaution);

            return false;
        }

        return true;
    }

    private bool CanEvolveAny(Entity<XenoEvolutionComponent> xeno)
    {
        if (xeno.Comp.Points >= xeno.Comp.Max && xeno.Comp.EvolvesTo.Count > 0)
            return true;

        if (xeno.Comp.Points >= xeno.Comp.Max && !IsWeedKillerDeployed() && xeno.Comp.EarlyEvolvesTo.Count > 0)
            return true;

        foreach (var evolution in xeno.Comp.EvolvesToWithoutPoints)
        {
            if (CanEvolvePopup(xeno, evolution, false))
                return true;
        }

        return false;
    }

    // TODO RMC14 make this a property of the hive component
    // TODO RMC14 per-hive
    public int GetLiving<T>(Predicate<Entity<T>>? predicate = null) where T : IComponent
    {
        var total = 0;
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobStateQuery.TryComp(uid, out var mobState) &&
                _mobState.IsDead(uid, mobState))
            {
                continue;
            }

            if (predicate != null && !predicate((uid, comp)))
                continue;

            total++;
        }

        return total;
    }

    // TODO RMC14 make this a property of the hive component
    // TODO RMC14 per-hive
    public bool HasLiving<T>(int count, Predicate<Entity<T>>? predicate = null) where T : IComponent
    {
        if (count <= 0)
            return true;

        var total = 0;
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobStateQuery.TryComp(uid, out var mobState) &&
                _mobState.IsDead(uid, mobState))
            {
                continue;
            }

            if (predicate != null && !predicate((uid, comp)))
                continue;

            total++;

            if (total >= count)
                return true;
        }

        return false;
    }

    public FixedPoint2 AddPointsCapped(Entity<XenoEvolutionComponent?> evolution, FixedPoint2 points)
    {
        if (!Resolve(evolution, ref evolution.Comp, false))
            return FixedPoint2.Zero;

        var oldPoints = evolution.Comp.Points;
        evolution.Comp.Points += FixedPoint2.Min(evolution.Comp.Max, points);
        Dirty(evolution);

        return evolution.Comp.Points - oldPoints;
    }

    public void SetPoints(Entity<XenoEvolutionComponent> evolution, FixedPoint2 points)
    {
        evolution.Comp.Points = points;
        Dirty(evolution);
    }

    public bool NeedsOvipositor()
    {
        return _gameTicker.RoundDuration() > _evolutionPointsRequireOvipositorAfter;
    }

    public bool HasOvipositor()
    {
        return HasLiving<XenoEvolutionGranterComponent>(1, e => HasComp<XenoAttachedOvipositorComponent>(e));
    }

    public bool LackingOvipositor()
    {
        return NeedsOvipositor() && !HasOvipositor();
    }

    private EntityUid TransferXeno(EntityUid xeno, EntProtoId proto)
    {
        var coordinates = _transform.GetMoverCoordinates(xeno);
        var newXeno = Spawn(proto, coordinates);
        _xenoHive.SetSameHive(xeno, newXeno);

        if (_mind.TryGetMind(xeno, out var mindId, out _))
        {
            _mind.TransferTo(mindId, newXeno);
            _mind.UnVisit(mindId);
        }

        foreach (var held in _hands.EnumerateHeld(xeno))
        {
            _hands.TryDrop(xeno, held);
        }

        // TODO RMC14 this is a hack because climbing on a newly created entity does not work properly for the client
        var comp = EnsureComp<XenoNewlyEvolvedComponent>(newXeno);

        _doors.Clear();
        _entityLookup.GetEntitiesIntersecting(xeno, _doors);
        foreach (var id in _doors)
        {
            if (HasComp<DoorComponent>(id) || HasComp<AirlockComponent>(id))
                comp.StopCollide.Add(id);
        }

        var newRecently = EnsureComp<XenoRecentlyDevolvedComponent>(newXeno);
        if (TryComp(xeno, out XenoRecentlyDevolvedComponent? oldRecently))
        {
            foreach (var (id, time) in oldRecently.Recent)
            {
                newRecently.Recent[id] = time;
            }
        }

        if (Prototype(xeno)?.ID is { } oldId)
            newRecently.Recent[oldId] = _timing.CurTime;

        return newXeno;
    }

    private void TryDevolve(Entity<XenoDevolveComponent> xeno, EntProtoId to, bool damagedCheck = true)
    {
        if (damagedCheck && !DamagedCheckPopup(xeno))
            return;

        if (Devolve(xeno, to) is { } newXeno && _net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-evolution-devolve", ("xeno", newXeno)), newXeno, newXeno, PopupType.LargeCaution);
    }

    public EntityUid? Devolve(Entity<XenoDevolveComponent> xeno, EntProtoId to)
    {
        if (_net.IsClient ||
            !xeno.Comp.DevolvesTo.Contains(to))
        {
            return null;
        }

        var newXeno = TransferXeno(xeno, to);
        var ev = new XenoDevolvedEvent(xeno, newXeno);
        RaiseLocalEvent(newXeno, ref ev, true);

        _adminLog.Add(LogType.RMCDevolve, $"Xenonid {ToPrettyString(xeno)} devolved into {ToPrettyString(newXeno)}");

        Del(xeno.Owner);

        var afterEv = new AfterNewXenoEvolvedEvent();
        RaiseLocalEvent(newXeno, ref afterEv);

        return newXeno;
    }

    public override void Update(float frameTime)
    {
        var newly = EntityQueryEnumerator<XenoNewlyEvolvedComponent>();
        while (newly.MoveNext(out var uid, out var comp))
        {
            if (comp.TriedClimb)
            {
                _intersecting.Clear();
                _entityLookup.GetEntitiesIntersecting(uid, _intersecting);
                for (var i = comp.StopCollide.Count - 1; i >= 0; i--)
                {
                    var colliding = comp.StopCollide[i];
                    if (!_intersecting.Contains(colliding))
                        comp.StopCollide.RemoveAt(i);
                }

                if (comp.StopCollide.Count == 0)
                    RemCompDeferred<XenoNewlyEvolvedComponent>(uid);

                continue;
            }

            comp.TriedClimb = true;
            if (TryComp(uid, out ClimbingComponent? climbing))
            {
                _climbable.Clear();
                _entityLookup.GetEntitiesIntersecting(uid, _climbable);

                foreach (var intersecting in _climbable)
                {
                    if (HasComp<ClimbableComponent>(intersecting))
                    {
                        _climb.ForciblySetClimbing(uid, intersecting);
                        Dirty(uid, climbing);
                        break;
                    }
                }
            }
        }

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var roundDuration = _gameTicker.RoundDuration();
        var needsOvipositor = NeedsOvipositor();
        var hasGranter = needsOvipositor
            ? HasOvipositor()
            : HasLiving<XenoEvolutionGranterComponent>(1);
        if (needsOvipositor)
        {
            var granters = EntityQueryEnumerator<XenoEvolutionGranterComponent>();
            while (granters.MoveNext(out var uid, out var granter))
            {
                if (granter.GotOvipositorPopup)
                    continue;

                granter.GotOvipositorPopup = true;
                Dirty(uid, granter);

                _popup.PopupEntity("It is time to settle down and let your children grow.",
                    uid,
                    uid,
                    PopupType.LargeCaution
                );

                _xenoHive.AnnounceNeedsOvipositorToSameHive(uid);
            }
        }

        var evoBonus = FixedPoint2.Zero;
        var bonuses = EntityQueryEnumerator<EvolutionBonusComponent>();
        while (bonuses.MoveNext(out var comp))
        {
            evoBonus += comp.Amount;
        }

        FixedPoint2? evoOverride = null;
        var overrides = EntityQueryEnumerator<EvolutionOverrideComponent>();
        while (overrides.MoveNext(out var comp))
        {
            evoOverride = comp.Amount;
        }

        var evolution = EntityQueryEnumerator<XenoEvolutionComponent>();
        while (evolution.MoveNext(out var uid, out var comp))
        {
            if (comp.Max == FixedPoint2.Zero)
                continue;

            if (time < comp.LastPointsAt + TimeSpan.FromSeconds(1))
                continue;

            comp.LastPointsAt = time;
            Dirty(uid, comp);

            if (!comp.GotPopup && CanEvolveAny((uid, comp)))
            {
                comp.GotPopup = true;
                Dirty(uid, comp);

                _popup.PopupEntity(Loc.GetString("cm-xeno-evolution-ready"), uid, uid, PopupType.Large);
                _audio.PlayEntity(comp.EvolutionReadySound, uid, uid);
                continue;
            }
            var points = (_earlyEvoBoostBefore > _gameTicker.RoundDuration()) ? comp.EarlyPointsPerSecond : comp.PointsPerSecond;
            var gain = evoOverride ?? points + evoBonus;
            if (comp.Points < comp.Max || roundDuration < _evolutionAccumulatePointsBefore)
            {
                if (needsOvipositor && comp.RequiresGranter && !hasGranter)
                    continue;

                SetPoints((uid, comp), comp.Points + gain);
            }
            else if (comp.Points > comp.Max)
            {
                SetPoints((uid, comp), FixedPoint2.Max(comp.Points - gain, comp.Max));
            }
        }

        if (IsT3Unlocked())
        {
            var hiveQuery = EntityQueryEnumerator<HiveComponent>();
            while (hiveQuery.MoveNext(out var hiveId, out _))
            {
                if (!_hivesWithRaffleRan.Contains(hiveId))
                {
                    _hivesWithRaffleRan.Add(hiveId);
                    RunT3Raffle(hiveId);
                }
            }
        }
    }

    private bool CanRaffleEvolve(EntityUid uid, EntProtoId choice)
    {
        if (TerminatingOrDeleted(uid))
            return false;

        if (_mobState.IsDead(uid))
            return false;

        if (!_mind.TryGetMind(uid, out _, out _))
            return false;

        if (_container.IsEntityInContainer(uid))
            return false;

        if (TryComp<RestrictEvolveOffWeedsComponent>(uid, out var weedsComp))
        {
            var coordinates = _transform.GetMoverCoordinates(uid).SnapToGrid(EntityManager, _map);
            if (_transform.GetGrid(coordinates) is not { } gridUid ||
                !TryComp(gridUid, out MapGridComponent? grid) ||
                (!_xenoWeeds.IsOnWeeds((gridUid, grid), coordinates) && weedsComp.RestrictTime > _gameTicker.RoundDuration()))
            {
                return false;
            }
        }

        return true;
    }

    private void RunT3Raffle(EntityUid hiveId)
    {
        var entrants = new List<(EntityUid Uid, XenoEvolutionComponent Evo, EntProtoId Choice)>();
        var evoQuery = EntityQueryEnumerator<XenoEvolutionComponent, HiveMemberComponent>();
        while (evoQuery.MoveNext(out var uid, out var evo, out var member))
        {
            if (member.Hive != hiveId || evo.T3RaffleChoice == null)
                continue;

            if (!CanRaffleEvolve(uid, evo.T3RaffleChoice.Value))
                continue;

            entrants.Add((uid, evo, evo.T3RaffleChoice.Value));
        }

        if (entrants.Count == 0)
            return;

        var totalXenos = 0;
        var countQuery = EntityQueryEnumerator<XenoComponent, HiveMemberComponent>();
        while (countQuery.MoveNext(out var uid, out var xenoComp, out var member))
        {
            if (member.Hive != hiveId || _mobState.IsDead(uid))
                continue;

            if (xenoComp.CountedInSlots)
                totalXenos++;
        }

        var t3Limit = 0.2f;
        if (_xenoHive.TryGetTierLimit((hiveId, null), 3, out var configuredLimit))
            t3Limit = configuredLimit.Float();

        var maxT3s = Math.Max(1, (int)Math.Floor(totalXenos * t3Limit));

        _random.Shuffle(entrants);

        var winners = 0;
        foreach (var (uid, evo, choice) in entrants)
        {
            if (winners >= maxT3s)
                break;

            if (!_prototypes.HasIndex(choice))
                continue;

            if (!CanRaffleEvolve(uid, choice))
                continue;

            var newXeno = TransferXeno(uid, choice);
            var ev = new NewXenoEvolvedEvent((uid, evo), newXeno, false);
            RaiseLocalEvent(newXeno, ref ev, true);

            _adminLog.Add(LogType.RMCEvolve, $"Xenonid {ToPrettyString(uid)} won T3 raffle, evolved into {ToPrettyString(newXeno)}");
            Del(uid);

            _popup.PopupEntity(Loc.GetString("rmc-xeno-t3-raffle-won"), newXeno, newXeno, PopupType.Large);

            var afterEv = new AfterNewXenoEvolvedEvent();
            RaiseLocalEvent(newXeno, ref afterEv);

            winners++;
        }

        var resultMsg = Loc.GetString("rmc-xeno-t3-raffle-result", ("count", winners));
        _xenoAnnounce.AnnounceToHive(EntityUid.Invalid, hiveId, resultMsg);
    }
}
