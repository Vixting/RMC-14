using Content.Server.Power.EntitySystems;
using Content.Shared._RMC14.Xenonids.Biomass;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Xenonids.Biomass;

public sealed class RMCBiomassAnalyzerMachineSystem : SharedRMCBiomassAnalyzerSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCBiomassAnalyzerSystem _biomass = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private const string OrganSlotId = "RMCBiomassOrganSlot";
    private const int MaxQueueLength = 10;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCBiomassAnalyzerMachineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCBiomassAnalyzerMachineComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
    }

    private void OnBeforeUIOpen(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        _biomass.EnsureAnalyzer();
    }

    private void OnInteractUsing(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<RMCBiomassOrganComponent>(args.Used, out var organ))
            return;

        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("This machine has no power!", args.User);
            return;
        }

        args.Handled = true;

        if (ent.Comp.HeldOrgan != null)
        {
            _popup.PopupCursor("The analyzer's receptacle is already occupied!", args.User);
            return;
        }

        var container = _container.EnsureContainer<ContainerSlot>(ent.Owner, OrganSlotId);
        if (!_container.Insert(args.Used, container))
            return;

        ent.Comp.HeldOrgan = args.Used;
        ent.Comp.HeldOrganValue = organ.Value;
        Dirty(ent);

        _popup.PopupCursor("You insert the specimen into the analyzer's receptacle.", args.User);
        _audio.PlayPvs("/Audio/Machines/twobeep.ogg", ent);

        if (ent.Comp.AutoProcessOrgan)
            TryStartProcessing(ent, args.User);
    }

    protected override bool TryEjectOrgan(Entity<RMCBiomassAnalyzerMachineComponent> ent, EntityUid actor)
    {
        if (ent.Comp.HeldOrgan is not { } organ)
            return false;

        if (ent.Comp.PrintingUntil != null)
        {
            _popup.PopupEntity("The analyzer is busy!", ent, actor);
            return false;
        }

        var container = _container.EnsureContainer<ContainerSlot>(ent.Owner, OrganSlotId);
        _container.Remove(organ, container);

        ent.Comp.HeldOrgan = null;
        ent.Comp.HeldOrganValue = 0;
        Dirty(ent);

        _popup.PopupEntity("You eject the specimen from the analyzer.", ent, actor);
        return true;
    }

    protected override bool ToggleAutoProcess(Entity<RMCBiomassAnalyzerMachineComponent> ent, EntityUid actor)
    {
        ent.Comp.AutoProcessOrgan = !ent.Comp.AutoProcessOrgan;
        Dirty(ent);

        if (ent.Comp.AutoProcessOrgan)
            TryStartProcessing(ent, actor);

        return true;
    }

    private bool TryStartProcessing(Entity<RMCBiomassAnalyzerMachineComponent> ent, EntityUid? actor)
    {
        if (ent.Comp.HeldOrgan is not { } organ)
            return false;

        if (ent.Comp.PrintingUntil != null)
            return false;

        var container = _container.EnsureContainer<ContainerSlot>(ent.Owner, OrganSlotId);
        _container.Remove(organ, container);
        QueueDel(organ);

        ent.Comp.HeldOrgan = null;
        StartPrintingAnimation(ent);

        var message = $"The analyzer processes the specimen, extracting {ent.Comp.HeldOrganValue} biomass points.";
        if (actor is { } user)
            _popup.PopupEntity(message, ent, user);
        else
            _popup.PopupEntity(message, ent);

        _audio.PlayPvs("/Audio/Machines/twobeep.ogg", ent);
        return true;
    }

    protected override void FinishProcessingOrgan(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        if (ent.Comp.HeldOrganValue > 0)
        {
            _biomass.AddPoints(ent.Comp.HeldOrganValue);
            ent.Comp.HeldOrganValue = 0;
            Dirty(ent);
        }

        if (ent.Comp.AutoProcessOrgan)
            TryStartProcessing(ent, null);
    }

    protected override bool TryEnqueuePrint(Entity<RMCBiomassAnalyzerMachineComponent> ent, string upgradeId, int amount, EntityUid actor)
    {
        if (amount <= 0)
            return false;

        if (!_prototype.TryIndex<RMCBiomassUpgradePrototype>(upgradeId, out var upgrade))
            return false;

        if (!_biomass.HasSufficientClearance(upgrade))
        {
            _popup.PopupEntity("You don't have clearance for this upgrade!", ent, actor);
            return false;
        }

        var added = 0;
        for (var i = 0; i < amount; i++)
        {
            if (ent.Comp.PrintQueue.Count >= MaxQueueLength)
            {
                _popup.PopupEntity("The analyzer's print queue is full!", ent, actor);
                break;
            }

            ent.Comp.PrintQueue.Add(upgradeId);
            added++;
        }

        if (added == 0)
            return false;

        if (!ent.Comp.QueueProcessing)
        {
            ent.Comp.QueueProcessing = true;
            ent.Comp.NextQueueItemAt = null;
        }

        Dirty(ent);
        return true;
    }

    protected override bool ToggleQueue(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        ent.Comp.QueueProcessing = !ent.Comp.QueueProcessing;
        if (ent.Comp.QueueProcessing)
            ent.Comp.NextQueueItemAt = null;

        Dirty(ent);
        return true;
    }

    protected override bool RemoveFromQueue(Entity<RMCBiomassAnalyzerMachineComponent> ent, int index)
    {
        if (index < 0 || index >= ent.Comp.PrintQueue.Count)
            return false;

        ent.Comp.PrintQueue.RemoveAt(index);
        Dirty(ent);
        return true;
    }

    protected override void ProcessNextQueueItem(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        if (ent.Comp.PrintQueue.Count == 0)
        {
            ent.Comp.QueueProcessing = false;
            Dirty(ent);
            return;
        }

        var upgradeId = ent.Comp.PrintQueue[0];
        var spawnAt = Transform(ent.Owner).Coordinates;
        if (!_biomass.TryPurchaseUpgrade(upgradeId, spawnAt, out _))
        {
            ent.Comp.QueueProcessing = false;
            Dirty(ent);
            _popup.PopupEntity("The analyzer flashes red - insufficient biomass or clearance to continue printing!", ent);
            return;
        }

        ent.Comp.PrintQueue.RemoveAt(0);
        ent.Comp.NextQueueItemAt = Timing.CurTime + QueuePrintDelay;

        if (ent.Comp.PrintQueue.Count == 0)
            ent.Comp.QueueProcessing = false;

        Dirty(ent);
        _audio.PlayPvs("/Audio/Machines/twobeep.ogg", ent);
    }
}
