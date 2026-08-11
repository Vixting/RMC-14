using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCChemistryResearchComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ClearanceLevel = 1;

    [DataField, AutoNetworkedField]
    public bool ReachedXAccess;

    [DataField, AutoNetworkedField]
    public int Credits;

    [DataField, AutoNetworkedField]
    public List<ContractSlot> Contracts = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, List<DocumentEntry>> Documents = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, List<DocumentEntry>> Published = new();

    [DataField, AutoNetworkedField]
    public HashSet<string> IdentifiedIds = new();

    [DataField, AutoNetworkedField]
    public HashSet<string> LockedIds = new();

    [DataField, AutoNetworkedField]
    public TimeSpan NextReroll;

    [DataField, AutoNetworkedField]
    public bool PickedThisCycle;

    [DataField, AutoNetworkedField]
    public string? LastPickedContractReagent;

    [DataField, AutoNetworkedField]
    public TimeSpan NextAnnounceAt;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ContractSlot
{
    [DataField]
    public string Name = string.Empty;

    [DataField]
    public int Tier;

    [DataField]
    public string PropertyHintId = string.Empty;

    [DataField]
    public string IngredientHintId = string.Empty;

    [DataField]
    public bool Taken;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class DocumentEntry
{
    [DataField]
    public NetEntity Report;

    [DataField]
    public string Title = string.Empty;

    [DataField]
    public TimeSpan Time;
}
