using Content.Shared._RMC14.Chemistry.Centrifuge;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Chemistry.SmartFridge;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.TuringDispenser;

public abstract class SharedRMCTuringDispenserSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedRMCCentrifugeSystem _centrifuge = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedRMCSmartFridgeSystem _smartFridge = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly EntProtoId VialPrototype = "RMCVial";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCTuringDispenserComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCTuringDispenserComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<RMCTuringDispenserComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<RMCTuringDispenserComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);

        Subs.BuiEvents<RMCTuringDispenserComponent>(RMCTuringDispenserUi.Key,
            subs =>
            {
                subs.Event<RMCTuringDispenserRunProgramBuiMsg>(OnRunProgramMsg);
                subs.Event<RMCTuringDispenserSaveToMemoryBuiMsg>(OnSaveToMemoryMsg);
                subs.Event<RMCTuringDispenserClearMemoryBuiMsg>(OnClearMemoryMsg);
                subs.Event<RMCTuringDispenserEjectBoxBuiMsg>(OnEjectBoxMsg);
                subs.Event<RMCTuringDispenserEjectBeakerBuiMsg>(OnEjectBeakerMsg);
                subs.Event<RMCTuringDispenserDisposeBeakerBuiMsg>(OnDisposeBeakerMsg);
                subs.Event<RMCTuringDispenserSetMultiplierBuiMsg>(OnSetMultiplierMsg);
                subs.Event<RMCTuringDispenserSetCyclesBuiMsg>(OnSetCyclesMsg);
                subs.Event<RMCTuringDispenserToggleAutoRunBuiMsg>(OnToggleAutoRunMsg);
                subs.Event<RMCTuringDispenserToggleSmartLinkBuiMsg>(OnToggleSmartLinkMsg);
                subs.Event<RMCTuringDispenserToggleOutputModeBuiMsg>(OnToggleOutputModeMsg);
            });
    }

    private void OnMapInit(Entity<RMCTuringDispenserComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.InputBoxSlotId);
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.OutputBeakerSlotId);
        ent.Comp.NextRecharge = _timing.CurTime + ent.Comp.RechargeEvery;
    }

    private void OnInsertAttempt(Entity<RMCTuringDispenserComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Slot.ContainerSlot?.ID == ent.Comp.InputBoxSlotId && ent.Comp.Status == TuringDispenserStatus.Running)
            args.Cancelled = true;
    }

    private void OnEntInserted(Entity<RMCTuringDispenserComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.InputBoxSlotId)
        {
            _appearance.SetData(ent, TuringDispenserVisuals.HasBox, true);
            BuildProgram(ent, TuringDispenserProgram.Box, args.Entity);
        }
        else if (args.Container.ID == ent.Comp.OutputBeakerSlotId)
        {
            _appearance.SetData(ent, TuringDispenserVisuals.HasBeaker, true);
        }
        else
        {
            return;
        }

        // Matches CM's attackby: once both a box and a beaker are loaded, autorun starts the program for you.
        if (ent.Comp is { AutoRun: true, OutputMode: TuringDispenserOutputMode.Container } &&
            ent.Comp.Status is TuringDispenserStatus.Idle or TuringDispenserStatus.Finished &&
            _itemSlots.TryGetSlot(ent, ent.Comp.InputBoxSlotId, out var boxSlot) && boxSlot.ContainerSlot?.ContainedEntity != null &&
            _itemSlots.TryGetSlot(ent, ent.Comp.OutputBeakerSlotId, out var beakerSlot) && beakerSlot.ContainerSlot?.ContainedEntity != null)
        {
            StartProgram(ent);
        }
    }

    private void OnEntRemoved(Entity<RMCTuringDispenserComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.OutputBeakerSlotId)
        {
            _appearance.SetData(ent, TuringDispenserVisuals.HasBeaker, false);
            StopProgram(ent);
            return;
        }

        if (args.Container.ID != ent.Comp.InputBoxSlotId)
            return;

        _appearance.SetData(ent, TuringDispenserVisuals.HasBox, false);
        ent.Comp.BoxProgram.Clear();
        Dirty(ent);
        StopProgram(ent);
    }

    private void BuildProgram(Entity<RMCTuringDispenserComponent> ent, TuringDispenserProgram program, EntityUid box)
    {
        if (!TryComp(box, out StorageComponent? storage))
            return;

        var entries = new List<TuringProgramEntry>();
        foreach (var vial in storage.Container.ContainedEntities)
        {
            if (!_solution.TryGetSolution(vial, "beaker", out _, out var solution) ||
                solution.Contents.Count != 1)
            {
                continue;
            }

            var content = solution.Contents[0];
            ProtoId<ReagentPrototype> reagent = content.Reagent.Prototype;

            var index = entries.FindIndex(e => e.Reagent == reagent);
            if (index >= 0)
                entries[index] = entries[index] with { Amount = entries[index].Amount + content.Quantity };
            else
                entries.Add(new TuringProgramEntry(reagent, content.Quantity));
        }

        if (program == TuringDispenserProgram.Memory)
            ent.Comp.MemoryProgram = entries;
        else
            ent.Comp.BoxProgram = entries;

        Dirty(ent);
    }

    private void OnRunProgramMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserRunProgramBuiMsg args)
    {
        if (ent.Comp.OutputMode == TuringDispenserOutputMode.Centrifuge)
            return;

        StartProgram(ent);
    }

    public void StartProgram(Entity<RMCTuringDispenserComponent> ent)
    {
        var comp = ent.Comp;
        comp.ActiveProgram = comp.MemoryProgram.Count > 0 ? TuringDispenserProgram.Memory : TuringDispenserProgram.Box;
        var program = comp.ActiveProgram == TuringDispenserProgram.Memory ? comp.MemoryProgram : comp.BoxProgram;

        if (program.Count == 0)
        {
            SetStatus(ent, TuringDispenserStatus.Idle);
            return;
        }

        if (comp.OutputMode == TuringDispenserOutputMode.Container &&
            (!_itemSlots.TryGetSlot(ent, comp.OutputBeakerSlotId, out var slot) ||
             slot.ContainerSlot?.ContainedEntity is null))
        {
            SetStatus(ent, TuringDispenserStatus.Idle);
            return;
        }

        comp.Stage = 0;
        comp.Cycle = 0;
        comp.StageMissing = FixedPoint2.Zero;
        comp.Error = null;
        SetStatus(ent, TuringDispenserStatus.Running);
    }

    private void OnSaveToMemoryMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserSaveToMemoryBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.InputBoxSlotId, out var slot) ||
            slot.ContainerSlot?.ContainedEntity is not { } box)
        {
            return;
        }

        BuildProgram(ent, TuringDispenserProgram.Memory, box);
    }

    private void OnClearMemoryMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserClearMemoryBuiMsg args)
    {
        ent.Comp.MemoryProgram.Clear();
        ent.Comp.ActiveProgram = TuringDispenserProgram.Box;
        Dirty(ent);
        StopProgram(ent);
    }

    /// <summary>
    /// Resets the dispenser back to idle, matching CM's stop_program(): clears stage/cycle/error state and
    /// flushes the buffer if it was feeding a smartfridge.
    /// </summary>
    private void StopProgram(Entity<RMCTuringDispenserComponent> ent)
    {
        var comp = ent.Comp;
        comp.Stage = 0;
        comp.Cycle = 0;
        comp.StageMissing = FixedPoint2.Zero;
        comp.Error = null;

        if (comp.OutputMode == TuringDispenserOutputMode.SmartFridge)
            FlushBuffer(ent);

        SetStatus(ent, TuringDispenserStatus.Idle);
    }

    private void OnEjectBoxMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserEjectBoxBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.InputBoxSlotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(ent, slot, args.Actor, true);
    }

    private void OnEjectBeakerMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserEjectBeakerBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.OutputBeakerSlotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(ent, slot, args.Actor, true);
    }

    private void OnDisposeBeakerMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserDisposeBeakerBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.OutputBeakerSlotId, out var slot) ||
            slot.ContainerSlot?.ContainedEntity is not { } beaker ||
            !_solution.TryGetMixableSolution(beaker, out var solnEnt, out _))
        {
            return;
        }

        _solution.RemoveAllSolution(solnEnt.Value);
    }

    private void OnSetMultiplierMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserSetMultiplierBuiMsg args)
    {
        if (args.Multiplier <= FixedPoint2.Zero)
            return;

        ent.Comp.Multiplier = args.Multiplier;
        Dirty(ent);
    }

    private void OnSetCyclesMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserSetCyclesBuiMsg args)
    {
        if (args.Cycles <= 0)
            return;

        ent.Comp.CycleLimit = args.Cycles;
        Dirty(ent);
    }

    private void OnToggleAutoRunMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserToggleAutoRunBuiMsg args)
    {
        ent.Comp.AutoRun = !ent.Comp.AutoRun;
        Dirty(ent);
    }

    private void OnToggleSmartLinkMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserToggleSmartLinkBuiMsg args)
    {
        ent.Comp.SmartLink = !ent.Comp.SmartLink;
        Dirty(ent);
    }

    private void OnToggleOutputModeMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserToggleOutputModeBuiMsg args)
    {
        var comp = ent.Comp;

        if (comp.OutputMode != TuringDispenserOutputMode.Container)
            FlushBuffer(ent);

        comp.OutputMode = comp.OutputMode switch
        {
            TuringDispenserOutputMode.Container => HasNearbySmartFridge(ent)
                ? TuringDispenserOutputMode.SmartFridge
                : HasNearbyCentrifuge(ent)
                    ? TuringDispenserOutputMode.Centrifuge
                    : TuringDispenserOutputMode.Container,
            TuringDispenserOutputMode.SmartFridge => HasNearbyCentrifuge(ent)
                ? TuringDispenserOutputMode.Centrifuge
                : TuringDispenserOutputMode.Container,
            _ => TuringDispenserOutputMode.Container,
        };

        Dirty(ent);
    }

    private bool HasNearbySmartFridge(Entity<RMCTuringDispenserComponent> ent)
    {
        var fridges = _entityLookup.GetEntitiesInRange<RMCSmartFridgeComponent>(Transform(ent).Coordinates, ent.Comp.SmartFridgeRange);
        return fridges.Count > 0;
    }

    private bool HasNearbyCentrifuge(Entity<RMCTuringDispenserComponent> ent)
    {
        return _centrifuge.HasNearbyCentrifuge(Transform(ent).Coordinates, ent.Comp.CentrifugeRange);
    }

    private void FlushBuffer(Entity<RMCTuringDispenserComponent> ent)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.BufferSolution, out var bufferEnt, out var buffer) ||
            buffer.Volume <= FixedPoint2.Zero)
        {
            return;
        }

        var coords = Transform(ent).Coordinates;
        var hasFridge = HasNearbySmartFridge(ent);

        while (buffer.Volume > FixedPoint2.Zero)
        {
            var reagent = buffer.Contents[0].Reagent;
            var amount = FixedPoint2.Min(FixedPoint2.New(30), buffer.GetReagentQuantity(reagent));
            if (amount <= FixedPoint2.Zero)
                break;

            if (!hasFridge)
            {
                _solution.RemoveReagent(bufferEnt.Value, reagent, amount);
                continue;
            }

            var vial = Spawn(VialPrototype, coords);
            if (!_solution.TryGetSolution(vial, "beaker", out var vialSolnEnt, out _))
            {
                QueueDel(vial);
                _solution.RemoveReagent(bufferEnt.Value, reagent, amount);
                continue;
            }

            _solution.RemoveReagent(bufferEnt.Value, reagent, amount);
            _solution.TryAddReagent(vialSolnEnt.Value, reagent.Prototype, amount, data: reagent.Data);

            if (_rmcReagent.TryIndex(reagent.Prototype, out var reagentProto))
                _metaData.SetEntityName(vial, $"vial ({reagentProto.LocalizedName})");

            _smartFridge.TransferToNearby(coords, ent.Comp.SmartFridgeRange, vial);
        }
    }

    private bool TryGetTargetSolution(Entity<RMCTuringDispenserComponent> ent, out Entity<SolutionComponent>? solnEnt, out Solution? solution)
    {
        if (ent.Comp.OutputMode == TuringDispenserOutputMode.Container)
        {
            if (!_itemSlots.TryGetSlot(ent, ent.Comp.OutputBeakerSlotId, out var slot) ||
                slot.ContainerSlot?.ContainedEntity is not { } beaker ||
                !_solution.TryGetMixableSolution(beaker, out solnEnt, out solution))
            {
                solnEnt = null;
                solution = null;
                return false;
            }

            return true;
        }

        return _solution.TryGetSolution(ent.Owner, ent.Comp.BufferSolution, out solnEnt, out solution);
    }

    private void AdvanceProgram(Entity<RMCTuringDispenserComponent> ent)
    {
        var comp = ent.Comp;
        comp.Stage = 0;
        comp.StageMissing = FixedPoint2.Zero;

        if (comp.MemoryProgram.Count > 0 && comp.BoxProgram.Count > 0)
        {
            if (comp.ActiveProgram == TuringDispenserProgram.Box)
            {
                comp.Cycle++;
                comp.ActiveProgram = TuringDispenserProgram.Memory;
            }
            else
            {
                comp.ActiveProgram = TuringDispenserProgram.Box;
            }
        }
        else
        {
            comp.Cycle++;
        }

        if (comp.OutputMode == TuringDispenserOutputMode.SmartFridge)
            FlushBuffer(ent);

        Dirty(ent);
    }

    private void SetStatus(Entity<RMCTuringDispenserComponent> ent, TuringDispenserStatus status)
    {
        ent.Comp.Status = status;
        Dirty(ent);
        _appearance.SetData(ent, TuringDispenserVisuals.Status, status);
        DispenserUpdated(ent);
    }

    protected virtual void DispenserUpdated(Entity<RMCTuringDispenserComponent> ent)
    {
    }

    /// <summary>
    /// Exposes the dispenser's internal buffer solution to <c>SharedRMCCentrifugeSystem</c> when it's tethered
    /// and pulling directly from this dispenser instead of a beaker.
    /// </summary>
    public bool TryGetBufferSolution(Entity<RMCTuringDispenserComponent> ent, out Entity<SolutionComponent>? solnEnt, out Solution? solution)
    {
        return _solution.TryGetSolution(ent.Owner, ent.Comp.BufferSolution, out solnEnt, out solution);
    }

    public bool IsReadyForCentrifuge(Entity<RMCTuringDispenserComponent> ent)
    {
        return ent.Comp.OutputMode == TuringDispenserOutputMode.Centrifuge &&
               (ent.Comp.MemoryProgram.Count > 0 || ent.Comp.BoxProgram.Count > 0);
    }

    public void RequestRun(Entity<RMCTuringDispenserComponent> ent)
    {
        StartProgram(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var dispensers = EntityQueryEnumerator<RMCTuringDispenserComponent>();
        while (dispensers.MoveNext(out var uid, out var comp))
        {
            UpdateDispenser((uid, comp), time);
        }
    }

    private void UpdateDispenser(Entity<RMCTuringDispenserComponent> ent, TimeSpan time)
    {
        var comp = ent.Comp;

        if (time >= comp.NextRecharge)
        {
            comp.NextRecharge = time + comp.RechargeEvery;
            comp.Energy = FixedPoint2.Min(comp.MaxEnergy, comp.Energy + comp.RechargeAmount);
            Dirty(ent);
        }

        if (comp.Status is TuringDispenserStatus.Idle or TuringDispenserStatus.Finished or TuringDispenserStatus.Stuck)
            return;

        if (!TryGetTargetSolution(ent, out var targetSolnEnt, out var targetSolution))
        {
            SetStatus(ent, TuringDispenserStatus.Finished);
            return;
        }

        var guard = 0;
        while (guard++ < 64)
        {
            if (comp.Cycle >= comp.CycleLimit)
            {
                SetStatus(ent, TuringDispenserStatus.Finished);
                return;
            }

            if (targetSolution!.AvailableVolume <= FixedPoint2.Zero)
            {
                SetStatus(ent, TuringDispenserStatus.Finished);
                return;
            }

            var program = comp.ActiveProgram == TuringDispenserProgram.Memory ? comp.MemoryProgram : comp.BoxProgram;
            if (program.Count == 0)
            {
                SetStatus(ent, TuringDispenserStatus.Finished);
                return;
            }

            if (comp.Stage >= program.Count)
            {
                AdvanceProgram(ent);
                continue;
            }

            var entry = program[comp.Stage];
            var amount = comp.StageMissing > FixedPoint2.Zero ? comp.StageMissing : entry.Amount * comp.Multiplier;
            amount = FixedPoint2.Min(amount, targetSolution.AvailableVolume);

            if (amount <= FixedPoint2.Zero)
            {
                comp.Stage++;
                comp.StageMissing = FixedPoint2.Zero;
                continue;
            }

            var fulfilled = FixedPoint2.Zero;
            if (comp.SmartLink &&
                _smartFridge.TryDrainStock(Transform(ent).Coordinates, comp.SmartFridgeRange, entry.Reagent, amount, out var drained) &&
                drained > FixedPoint2.Zero)
            {
                _solution.TryAddReagent(targetSolnEnt!.Value, entry.Reagent, drained);
                fulfilled = drained;
            }

            if (fulfilled < amount)
            {
                var remaining = amount - fulfilled;
                if (comp.SynthesizableReagents.Contains(entry.Reagent))
                {
                    var cost = remaining * comp.CostPerUnit;
                    if (comp.Energy < cost)
                    {
                        comp.StageMissing = fulfilled > FixedPoint2.Zero ? remaining : FixedPoint2.Zero;
                        Dirty(ent);
                        return;
                    }

                    comp.Energy -= cost;
                    _solution.TryAddReagent(targetSolnEnt!.Value, entry.Reagent, remaining);
                    fulfilled += remaining;
                }
                else if (fulfilled <= FixedPoint2.Zero)
                {
                    comp.Status = TuringDispenserStatus.Stuck;
                    comp.Error = $"{entry.Reagent.Id} NOT FOUND";
                    Dirty(ent);
                    return;
                }
            }

            comp.Stage++;
            comp.StageMissing = FixedPoint2.Zero;
            Dirty(ent);
        }
    }
}
