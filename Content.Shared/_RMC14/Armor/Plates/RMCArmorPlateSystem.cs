using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared.Actions;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor.Plates;

public sealed class RMCArmorPlateSystem : EntitySystem
{
    public const string PlateSlotId = "rmc_armor_plate_slot";

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCArmorPlateSlotComponent, EntInsertedIntoContainerMessage>(OnPlateChanged);
        SubscribeLocalEvent<RMCArmorPlateSlotComponent, EntRemovedFromContainerMessage>(OnPlateChanged);
        SubscribeLocalEvent<RMCArmorPlateSlotComponent, GotEquippedEvent>(OnSlotEquipped);
        SubscribeLocalEvent<RMCArmorPlateSlotComponent, GotUnequippedEvent>(OnSlotUnequipped);

        SubscribeLocalEvent<RMCCoagulatorPlateActiveComponent, CMBleedAttemptEvent>(OnCoagulatorBleedAttempt);
        SubscribeLocalEvent<RMCAntiDecayPlateActiveComponent, MobStateChangedEvent>(OnAntiDecayMobStateChanged, after: [typeof(RMCUnrevivableSystem)]);
        SubscribeLocalEvent<RMCEmergencyInjectorPlateActiveComponent, RMCEmergencyInjectorInjectActionEvent>(OnEmergencyInjectorInject);
        SubscribeLocalEvent<RMCEmergencyInjectorPlateActiveComponent, RMCEmergencyInjectorToggleOverdoseActionEvent>(OnEmergencyInjectorToggleOverdose);
        SubscribeLocalEvent<RMCCeramicPlateActiveComponent, DamageModifyEvent>(OnCeramicDamageModify);
    }

    private void OnPlateChanged(Entity<RMCArmorPlateSlotComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == PlateSlotId)
            RefreshWearerEffect(ent);
    }

    private void OnPlateChanged(Entity<RMCArmorPlateSlotComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == PlateSlotId)
            RefreshWearerEffect(ent);
    }

    private void OnSlotEquipped(Entity<RMCArmorPlateSlotComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.Wearer = args.Equipee;
        Dirty(ent);
        RefreshWearerEffect(ent);
    }

    private void OnSlotUnequipped(Entity<RMCArmorPlateSlotComponent> ent, ref GotUnequippedEvent args)
    {
        RemoveAllWearerMarkers(args.Equipee);
        ent.Comp.Wearer = null;
        Dirty(ent);
    }

    private void RefreshWearerEffect(Entity<RMCArmorPlateSlotComponent> ent)
    {
        if (ent.Comp.Wearer is not { } wearer)
            return;

        RemoveAllWearerMarkers(wearer);

        if (!TryGetPlate(ent, out var kind, out var magnitude))
            return;

        switch (kind)
        {
            case RMCArmorPlateKind.Coagulator:
                var coagulator = EnsureComp<RMCCoagulatorPlateActiveComponent>(wearer);
                coagulator.CancelChance = magnitude;
                Dirty(wearer, coagulator);
                break;
            case RMCArmorPlateKind.AntiDecay:
                var antiDecay = EnsureComp<RMCAntiDecayPlateActiveComponent>(wearer);
                antiDecay.BonusTime = TimeSpan.FromMinutes(magnitude);
                Dirty(wearer, antiDecay);
                break;
            case RMCArmorPlateKind.EmergencyInjector:
                var injector = EnsureComp<RMCEmergencyInjectorPlateActiveComponent>(wearer);
                injector.Plate = GetPlateEntity(ent);
                _actions.AddAction(wearer, ref injector.InjectActionEntity, injector.InjectAction);
                _actions.AddAction(wearer, ref injector.ToggleActionEntity, injector.ToggleAction);
                Dirty(wearer, injector);
                break;
            case RMCArmorPlateKind.Ceramic:
                var ceramic = EnsureComp<RMCCeramicPlateActiveComponent>(wearer);
                ceramic.Plate = GetPlateEntity(ent);
                Dirty(wearer, ceramic);
                break;
        }
    }

    private void RemoveAllWearerMarkers(EntityUid wearer)
    {
        if (TryComp(wearer, out RMCEmergencyInjectorPlateActiveComponent? injector))
        {
            _actions.RemoveAction(wearer, injector.InjectActionEntity);
            _actions.RemoveAction(wearer, injector.ToggleActionEntity);
        }

        RemComp<RMCCoagulatorPlateActiveComponent>(wearer);
        RemComp<RMCAntiDecayPlateActiveComponent>(wearer);
        RemComp<RMCEmergencyInjectorPlateActiveComponent>(wearer);
        RemComp<RMCCeramicPlateActiveComponent>(wearer);
    }

    private bool TryGetPlate(Entity<RMCArmorPlateSlotComponent> ent, out RMCArmorPlateKind kind, out int magnitude)
    {
        kind = default;
        magnitude = 0;

        if (!_container.TryGetContainer(ent.Owner, PlateSlotId, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            return false;
        }

        if (!TryComp(container.ContainedEntities[0], out RMCArmorPlateComponent? plate))
            return false;

        kind = plate.Kind;
        magnitude = plate.Magnitude;
        return true;
    }

    private EntityUid? GetPlateEntity(Entity<RMCArmorPlateSlotComponent> ent)
    {
        if (_container.TryGetContainer(ent.Owner, PlateSlotId, out var container) &&
            container.ContainedEntities.Count > 0)
        {
            return container.ContainedEntities[0];
        }

        return null;
    }

    private void OnCoagulatorBleedAttempt(Entity<RMCCoagulatorPlateActiveComponent> ent, ref CMBleedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.CancelChance >= 100)
            args.Cancelled = true;
    }

    private void OnAntiDecayMobStateChanged(Entity<RMCAntiDecayPlateActiveComponent> ent, ref MobStateChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.NewMobState != MobState.Dead)
            return;

        _unrevivable.AddRevivableTime(ent.Owner, ent.Comp.BonusTime);
    }

    private void OnEmergencyInjectorInject(Entity<RMCEmergencyInjectorPlateActiveComponent> ent, ref RMCEmergencyInjectorInjectActionEvent args)
    {
        if (_net.IsClient)
            return;

        var wearer = ent.Owner;
        if (ent.Comp.Plate is not { } plateUid || !TryComp(plateUid, out RMCEmergencyInjectorPlateComponent? plate))
            return;

        if (plate.Used)
        {
            _popup.PopupEntity("The plate's reserve is empty - replace it.", wearer, wearer, PopupType.SmallCaution);
            return;
        }

        if (!_solution.TryGetInjectableSolution(wearer, out var solutionEnt, out var solution))
            return;

        if (plate.OverdoseProtection == RMCEmergencyInjectorOverdose.Strict)
        {
            foreach (var (reagent, amount) in plate.Cocktail)
            {
                if (_rmcReagent.TryIndex(reagent, out var proto) && proto.Overdose is { } od &&
                    solution.GetTotalPrototypeQuantity(reagent) + amount > od)
                {
                    _popup.PopupEntity("The plate buzzes and refuses to inject - overdose risk!", wearer, wearer, PopupType.MediumCaution);
                    return;
                }
            }
        }

        var toInject = new Solution();
        var adjusted = false;
        var overdosed = false;
        foreach (var (reagent, amount) in plate.Cocktail)
        {
            var add = amount;
            if (_rmcReagent.TryIndex(reagent, out var proto) && proto.Overdose is { } overdose)
            {
                var current = solution.GetTotalPrototypeQuantity(reagent);
                if (current + add > overdose)
                {
                    if (plate.OverdoseProtection == RMCEmergencyInjectorOverdose.Dynamic)
                    {
                        add = FixedPoint2.Max(FixedPoint2.Zero, overdose - current);
                        adjusted = true;
                    }
                    else
                    {
                        overdosed = true;
                    }
                }
            }

            if (add > FixedPoint2.Zero)
                toInject.AddReagent(reagent, add);
        }

        if (toInject.Volume <= FixedPoint2.Zero)
            return;

        plate.Used = true;
        Dirty(plateUid, plate);

        _reactive.DoEntityReaction(wearer, toInject, ReactionMethod.Injection);
        _solution.TryAddSolution(solutionEnt.Value, toInject);

        if (overdosed)
            _popup.PopupEntity("The plate injects the cocktail with a worrying beep - overdose!", wearer, wearer, PopupType.MediumCaution);
        else if (adjusted)
            _popup.PopupEntity("The plate injects the cocktail with a relieving beep - amounts adjusted to prevent overdose.", wearer, wearer);
        else
            _popup.PopupEntity("The plate injects its emergency cocktail.", wearer, wearer);

        args.Handled = true;
    }

    private void OnEmergencyInjectorToggleOverdose(Entity<RMCEmergencyInjectorPlateActiveComponent> ent, ref RMCEmergencyInjectorToggleOverdoseActionEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Plate is not { } plateUid || !TryComp(plateUid, out RMCEmergencyInjectorPlateComponent? plate))
            return;

        plate.OverdoseProtection = plate.OverdoseProtection switch
        {
            RMCEmergencyInjectorOverdose.Dynamic => RMCEmergencyInjectorOverdose.Off,
            RMCEmergencyInjectorOverdose.Off => RMCEmergencyInjectorOverdose.Strict,
            _ => RMCEmergencyInjectorOverdose.Dynamic,
        };
        Dirty(plateUid, plate);

        var text = plate.OverdoseProtection switch
        {
            RMCEmergencyInjectorOverdose.Dynamic => "Overdose protection set to DYNAMIC - amounts capped to avoid overdose.",
            RMCEmergencyInjectorOverdose.Strict => "Overdose protection set to STRICT - will refuse if any reagent would overdose.",
            _ => "Overdose protection set to OFF - full dose regardless of overdose.",
        };
        _popup.PopupEntity(text, ent.Owner, ent.Owner);

        args.Handled = true;
    }

    private void OnCeramicDamageModify(Entity<RMCCeramicPlateActiveComponent> ent, ref DamageModifyEvent args)
    {
        if (args.Tool is not { } tool || !HasComp<ProjectileComponent>(tool))
            return;

        if (ent.Comp.Plate is not { } plateUid || !TryComp(plateUid, out RMCCeramicPlateComponent? plate) || plate.Broken)
            return;

        var incoming = args.Damage.GetTotal();
        if (incoming <= FixedPoint2.Zero)
            return;

        args.Damage = new DamageSpecifier();

        if (_net.IsClient)
            return;

        var mult = plate.HostileDurabilityMult;
        if (args.Origin is { } origin && _faction.IsEntityFriendly(ent.Owner, origin))
            mult = plate.FriendlyFireDurabilityMult;

        plate.Health -= incoming.Float() * mult;

        if (plate.Health <= 0)
        {
            plate.Health = 0;
            plate.Broken = true;
            _popup.PopupEntity("Your ceramic plate shatters!", ent.Owner, ent.Owner, PopupType.MediumCaution);
        }

        UpdateCeramicVisuals(plateUid, plate);
        Dirty(plateUid, plate);
    }

    private void UpdateCeramicVisuals(EntityUid plateUid, RMCCeramicPlateComponent plate)
    {
        var state = RMCCeramicPlateVisualState.Full;
        if (plate.Broken || plate.Health <= 0)
            state = RMCCeramicPlateVisualState.Broken;
        else if (plate.MaxHealth > 0 && plate.Health <= plate.MaxHealth / 2f)
            state = RMCCeramicPlateVisualState.Damaged;

        _appearance.SetData(plateUid, RMCCeramicPlateVisuals.State, state);
    }
}
