using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Biomass;

public abstract class SharedRMCBiomassAnalyzerSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly TimeSpan PrintingDuration = TimeSpan.FromSeconds(3);
    protected static readonly TimeSpan QueuePrintDelay = TimeSpan.FromSeconds(2);

    public override void Initialize()
    {
        Subs.BuiEvents<RMCBiomassAnalyzerMachineComponent>(RMCBiomassAnalyzerUi.Key,
            subs =>
            {
                subs.Event<RMCBiomassAnalyzerEjectOrganBuiMsg>(OnEjectOrganMsg);
                subs.Event<RMCBiomassAnalyzerToggleAutoProcessBuiMsg>(OnToggleAutoProcessMsg);
                subs.Event<RMCBiomassAnalyzerEnqueuePrintBuiMsg>(OnEnqueuePrintMsg);
                subs.Event<RMCBiomassAnalyzerToggleQueueBuiMsg>(OnToggleQueueMsg);
                subs.Event<RMCBiomassAnalyzerRemoveFromQueueBuiMsg>(OnRemoveFromQueueMsg);
            });
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<RMCBiomassAnalyzerMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var ent = (Entity<RMCBiomassAnalyzerMachineComponent>) (uid, comp);

            if (comp.PrintingUntil is { } printingUntil && Timing.CurTime >= printingUntil)
            {
                comp.PrintingUntil = null;
                Dirty(uid, comp);
                UpdateAppearance(ent);
                FinishProcessingOrgan(ent);
            }

            if (comp.QueueProcessing && comp.PrintQueue.Count > 0 &&
                (comp.NextQueueItemAt is not { } nextAt || Timing.CurTime >= nextAt))
            {
                ProcessNextQueueItem(ent);
            }
        }
    }

    protected void StartPrintingAnimation(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        ent.Comp.PrintingUntil = Timing.CurTime + PrintingDuration;
        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        var state = ent.Comp.PrintingUntil != null
            ? RMCBiomassAnalyzerVisualState.Printing
            : RMCBiomassAnalyzerVisualState.Idle;

        Appearance.SetData(ent, RMCBiomassAnalyzerVisuals.State, state);
    }

    private void OnEjectOrganMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerEjectOrganBuiMsg args)
    {
        if (TryEjectOrgan(ent, args.Actor))
            RefreshUIs(ent);
    }

    private void OnToggleAutoProcessMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerToggleAutoProcessBuiMsg args)
    {
        if (ToggleAutoProcess(ent, args.Actor))
            RefreshUIs(ent);
    }

    private void OnEnqueuePrintMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerEnqueuePrintBuiMsg args)
    {
        if (TryEnqueuePrint(ent, args.UpgradeId, args.Amount, args.Actor))
            RefreshUIs(ent);
    }

    private void OnToggleQueueMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerToggleQueueBuiMsg args)
    {
        if (ToggleQueue(ent))
            RefreshUIs(ent);
    }

    private void OnRemoveFromQueueMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerRemoveFromQueueBuiMsg args)
    {
        if (RemoveFromQueue(ent, args.Index))
            RefreshUIs(ent);
    }

    protected virtual bool TryEjectOrgan(Entity<RMCBiomassAnalyzerMachineComponent> ent, EntityUid actor)
    {
        return false;
    }

    protected virtual bool ToggleAutoProcess(Entity<RMCBiomassAnalyzerMachineComponent> ent, EntityUid actor)
    {
        return false;
    }

    protected virtual bool TryEnqueuePrint(Entity<RMCBiomassAnalyzerMachineComponent> ent, string upgradeId, int amount, EntityUid actor)
    {
        return false;
    }

    protected virtual bool ToggleQueue(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        return false;
    }

    protected virtual bool RemoveFromQueue(Entity<RMCBiomassAnalyzerMachineComponent> ent, int index)
    {
        return false;
    }

    protected virtual void FinishProcessingOrgan(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
    }

    protected virtual void ProcessNextQueueItem(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
    }

    protected virtual void RefreshUIs(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
    }

    public bool TryGetAnalyzer(out RMCBiomassAnalyzerComponent analyzer)
    {
        var query = EntityQueryEnumerator<RMCBiomassAnalyzerComponent>();
        if (query.MoveNext(out _, out var comp))
        {
            analyzer = comp;
            return true;
        }

        analyzer = default!;
        return false;
    }
}
