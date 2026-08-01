using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Armor.Plates;

public sealed class RMCArmorPlateSystem : EntitySystem
{
    public const string PlateSlotId = "rmc_armor_plate_slot";

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCArmorPlateSlotComponent, EntInsertedIntoContainerMessage>(OnPlateChanged);
        SubscribeLocalEvent<RMCArmorPlateSlotComponent, EntRemovedFromContainerMessage>(OnPlateChanged);
        SubscribeLocalEvent<RMCArmorPlateSlotComponent, GotEquippedEvent>(OnSlotEquipped);
        SubscribeLocalEvent<RMCArmorPlateSlotComponent, GotUnequippedEvent>(OnSlotUnequipped);

        SubscribeLocalEvent<RMCArmorPlateSlotComponent, CMGetArmorEvent>(OnGetArmor);
        SubscribeLocalEvent<RMCArmorPlateSlotComponent, InventoryRelayedEvent<CMGetArmorEvent>>(OnGetArmorRelayed);

        SubscribeLocalEvent<RMCCoagulatorPlateActiveComponent, CMBleedAttemptEvent>(OnCoagulatorBleedAttempt);
        SubscribeLocalEvent<RMCAntiDecayPlateActiveComponent, DamageModifyEvent>(OnAntiDecayDamageModify);
        SubscribeLocalEvent<RMCEmergencyInjectorPlateActiveComponent, MobStateChangedEvent>(OnEmergencyInjectorMobStateChanged);
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
                antiDecay.DamageMultiplier = 1f - magnitude / 100f;
                Dirty(wearer, antiDecay);
                break;
            case RMCArmorPlateKind.EmergencyInjector:
                var injector = EnsureComp<RMCEmergencyInjectorPlateActiveComponent>(wearer);
                injector.Amount = magnitude;
                Dirty(wearer, injector);
                break;
            case RMCArmorPlateKind.Ceramic:
                break;
        }
    }

    private void RemoveAllWearerMarkers(EntityUid wearer)
    {
        RemComp<RMCCoagulatorPlateActiveComponent>(wearer);
        RemComp<RMCAntiDecayPlateActiveComponent>(wearer);
        RemComp<RMCEmergencyInjectorPlateActiveComponent>(wearer);
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

    private void OnGetArmor(Entity<RMCArmorPlateSlotComponent> ent, ref CMGetArmorEvent args)
    {
        ApplyCeramicArmor(ent, ref args);
    }

    private void OnGetArmorRelayed(Entity<RMCArmorPlateSlotComponent> ent, ref InventoryRelayedEvent<CMGetArmorEvent> args)
    {
        ApplyCeramicArmor(ent, ref args.Args);
    }

    private void ApplyCeramicArmor(Entity<RMCArmorPlateSlotComponent> ent, ref CMGetArmorEvent args)
    {
        if (!TryGetPlate(ent, out var kind, out var magnitude) || kind != RMCArmorPlateKind.Ceramic)
            return;

        args.Melee += magnitude;
        args.Bullet += magnitude;
        args.Bio += magnitude;
    }

    private void OnCoagulatorBleedAttempt(Entity<RMCCoagulatorPlateActiveComponent> ent, ref CMBleedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_random.Prob(ent.Comp.CancelChance / 100f))
            args.Cancelled = true;
    }

    private void OnAntiDecayDamageModify(Entity<RMCAntiDecayPlateActiveComponent> ent, ref DamageModifyEvent args)
    {
        if (args.Origin is not { } origin || !HasComp<XenoAcidComponent>(origin))
            return;

        args.Damage = args.Damage * ent.Comp.DamageMultiplier;
    }

    private void OnEmergencyInjectorMobStateChanged(Entity<RMCEmergencyInjectorPlateActiveComponent> ent, ref MobStateChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.NewMobState != MobState.Critical)
            return;

        if (_timing.CurTime < ent.Comp.NextUse)
            return;

        if (!_solution.TryGetInjectableSolution(ent.Owner, out var solutionEnt, out var solution))
            return;

        var toInject = new Solution();
        toInject.AddReagent(ent.Comp.Reagent, ent.Comp.Amount);

        if (!solution.CanAddSolution(toInject))
            return;

        ent.Comp.NextUse = _timing.CurTime + ent.Comp.Cooldown;
        Dirty(ent);

        _reactive.DoEntityReaction(ent.Owner, toInject, ReactionMethod.Injection);
        _solution.TryAddSolution(solutionEnt.Value, toInject);
    }
}
