using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

[Serializable, NetSerializable]
public enum RMCResearchComputerUi : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerBrokerClearanceBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCResearchComputerRequestXAccessBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCResearchComputerReprintContractBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCResearchComputerTakeContractBuiMsg : BoundUserInterfaceMessage
{
    public int Index;
    public RMCResearchComputerTakeContractBuiMsg(int index) => Index = index;
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerReadDocumentBuiMsg : BoundUserInterfaceMessage
{
    public string Category;
    public string Title;
    public bool Published;
    public RMCResearchComputerReadDocumentBuiMsg(string category, string title, bool published)
    {
        Category = category;
        Title = title;
        Published = published;
    }
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerPrintDocumentBuiMsg : BoundUserInterfaceMessage
{
    public string Category;
    public string Title;
    public bool Published;
    public RMCResearchComputerPrintDocumentBuiMsg(string category, string title, bool published)
    {
        Category = category;
        Title = title;
        Published = published;
    }
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerPublishDocumentBuiMsg : BoundUserInterfaceMessage
{
    public string Category;
    public string Title;
    public RMCResearchComputerPublishDocumentBuiMsg(string category, string title)
    {
        Category = category;
        Title = title;
    }
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerUnpublishDocumentBuiMsg : BoundUserInterfaceMessage
{
    public string Category;
    public string Title;
    public RMCResearchComputerUnpublishDocumentBuiMsg(string category, string title)
    {
        Category = category;
        Title = title;
    }
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerAnnounceDocumentBuiMsg : BoundUserInterfaceMessage
{
    public string Category;
    public string Title;
    public RMCResearchComputerAnnounceDocumentBuiMsg(string category, string title)
    {
        Category = category;
        Title = title;
    }
}
