namespace Content.Shared._RMC14.Chemistry.Generation;

public abstract class SharedRMCResearchComputerSystem : EntitySystem
{
    public override void Initialize()
    {
        Subs.BuiEvents<RMCResearchComputerComponent>(RMCResearchComputerUi.Key,
            subs =>
            {
                subs.Event<RMCResearchComputerBrokerClearanceBuiMsg>(OnBrokerClearance);
                subs.Event<RMCResearchComputerRequestXAccessBuiMsg>(OnRequestXAccess);
                subs.Event<RMCResearchComputerTakeContractBuiMsg>(OnTakeContract);
                subs.Event<RMCResearchComputerReprintContractBuiMsg>(OnReprintContract);
                subs.Event<RMCResearchComputerReadDocumentBuiMsg>(OnReadDocument);
                subs.Event<RMCResearchComputerPrintDocumentBuiMsg>(OnPrintDocument);
                subs.Event<RMCResearchComputerPublishDocumentBuiMsg>(OnPublishDocument);
                subs.Event<RMCResearchComputerUnpublishDocumentBuiMsg>(OnUnpublishDocument);
                subs.Event<RMCResearchComputerAnnounceDocumentBuiMsg>(OnAnnounceDocument);
            });
    }

    private void OnBrokerClearance(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerBrokerClearanceBuiMsg args)
    {
        TryBrokerClearance(ent, args.Actor);
        RefreshUIs(ent);
    }

    private void OnRequestXAccess(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerRequestXAccessBuiMsg args)
    {
        TryBrokerXAccess(ent, args.Actor);
        RefreshUIs(ent);
    }

    private void OnTakeContract(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerTakeContractBuiMsg args)
    {
        TryTakeContract(ent, args.Index);
        RefreshUIs(ent);
    }

    private void OnReprintContract(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerReprintContractBuiMsg args)
    {
        ReprintContract(ent);
        RefreshUIs(ent);
    }

    private void OnReadDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerReadDocumentBuiMsg args)
        => ReadDocument(ent, args.Category, args.Title, args.Published, args.Actor);

    private void OnPrintDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerPrintDocumentBuiMsg args)
        => PrintDocument(ent, args.Category, args.Title, args.Published);

    private void OnPublishDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerPublishDocumentBuiMsg args)
    {
        TryPublishDocument(args.Category, args.Title);
        RefreshUIs(ent);
    }

    private void OnUnpublishDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerUnpublishDocumentBuiMsg args)
    {
        TryUnpublishDocument(args.Category, args.Title);
        RefreshUIs(ent);
    }

    private void OnAnnounceDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerAnnounceDocumentBuiMsg args)
    {
        AnnounceDocument(ent, args.Category, args.Title, args.Actor);
        RefreshUIs(ent);
    }

    protected virtual void RefreshUIs(Entity<RMCResearchComputerComponent> ent)
    {
    }

    protected virtual bool TryBrokerClearance(Entity<RMCResearchComputerComponent> ent, EntityUid actor) => false;
    protected virtual bool TryBrokerXAccess(Entity<RMCResearchComputerComponent> ent, EntityUid actor) => false;
    protected virtual bool TryTakeContract(Entity<RMCResearchComputerComponent> ent, int slotIndex) => false;
    protected virtual void ReprintContract(Entity<RMCResearchComputerComponent> ent) { }
    protected virtual void ReadDocument(Entity<RMCResearchComputerComponent> ent, string category, string title, bool published, EntityUid actor) { }
    protected virtual void PrintDocument(Entity<RMCResearchComputerComponent> ent, string category, string title, bool published) { }
    protected virtual bool TryPublishDocument(string category, string title) => false;
    protected virtual bool TryUnpublishDocument(string category, string title) => false;
    protected virtual void AnnounceDocument(Entity<RMCResearchComputerComponent> ent, string category, string title, EntityUid actor) { }
}
