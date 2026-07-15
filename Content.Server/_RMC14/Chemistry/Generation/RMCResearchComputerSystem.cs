using Content.Server.Power.EntitySystems;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Intel;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCResearchComputerSystem : SharedRMCResearchComputerSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCChemistryResearchSystem _research = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCResearchComputerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCResearchComputerComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
    }

    private void OnBeforeUIOpen(Entity<RMCResearchComputerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        Log.Debug($"OnBeforeUIOpen: {ToPrettyString(ent)} opening research computer UI");
        var research = _research.EnsureResearch();
        Log.Debug($"OnBeforeUIOpen: research singleton {ToPrettyString(research.Owner)}, Contracts.Count={research.Comp.Contracts.Count}");
    }

    private void OnInteractUsing(Entity<RMCResearchComputerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var isRerollDisk = HasComp<RMCResearchRerollDiskComponent>(args.Used);
        if (!isRerollDisk && !HasComp<PaperComponent>(args.Used))
            return;

        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("This terminal has no power!", args.User);
            return;
        }

        if (isRerollDisk)
        {
            _research.ForceRerollContracts();
            QueueDel(args.Used);
            _popup.PopupCursor("The terminal accepts the disk, and rerolls the contract chemicals.", args.User);
            _audio.PlayPvs("/Audio/Machines/twobeep.ogg", ent);
            args.Handled = true;
            return;
        }

        if (TryComp<IntelChemGrantPaperComponent>(args.Used, out var grant))
        {
            _research.AddCredits(grant.Grant);
            QueueDel(args.Used);
            _popup.PopupCursor($"Research grant redeemed for {grant.Grant} credits.", args.User);
            _audio.PlayPvs("/Audio/Machines/twobeep.ogg", ent);
            args.Handled = true;
            return;
        }

        if (!_research.TrySaveManualScan(args.Used, Transform(ent).Coordinates))
            return;

        _popup.PopupCursor("The terminal scans the document and files it under Manual Scans.", args.User);
        args.Handled = true;
    }

    protected override bool TryBrokerClearance(Entity<RMCResearchComputerComponent> ent, EntityUid actor)
    {
        return _research.TryRaiseClearance(Transform(ent).Coordinates);
    }

    protected override bool TryBrokerXAccess(Entity<RMCResearchComputerComponent> ent, EntityUid actor)
    {
        return _research.TryRequestXAccess();
    }

    protected override bool TryTakeContract(Entity<RMCResearchComputerComponent> ent, int slotIndex)
    {
        return _research.TryTakeContract(slotIndex, Transform(ent).Coordinates);
    }

    protected override void ReprintContract(Entity<RMCResearchComputerComponent> ent)
    {
        _research.TryReprintContract(Transform(ent).Coordinates);
    }

    protected override void ReadDocument(Entity<RMCResearchComputerComponent> ent, string category, string title, bool published, EntityUid actor)
    {
        if (!_research.TryGetDocument(category, title, published, out var netEntity))
            return;

        if (GetEntity(netEntity) is not { Valid: true } report)
            return;

        _ui.OpenUi(report, PaperComponent.PaperUiKey.Key, actor);
    }

    protected override void PrintDocument(Entity<RMCResearchComputerComponent> ent, string category, string title, bool published)
    {
        _research.TryPrintDocument(category, title, published, Transform(ent).Coordinates);
    }

    protected override bool TryPublishDocument(string category, string title)
    {
        return _research.PublishDocument(category, title);
    }

    protected override bool TryUnpublishDocument(string category, string title)
    {
        return _research.UnpublishDocument(category, title);
    }

    protected override void AnnounceDocument(Entity<RMCResearchComputerComponent> ent, string category, string title, EntityUid actor)
    {
        _research.AnnounceDocument(ent.Owner, category, title, actor);
    }
}
