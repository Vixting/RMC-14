using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[Serializable, NetSerializable]
public enum XenoEvolutionUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class XenoEvolveBuiState(
    bool lackingOvipositor,
    bool weedKillerDeployed,
    bool hasHiveQueen,
    bool t3Unlocked) : BoundUserInterfaceState
{
    public readonly bool LackingOvipositor = lackingOvipositor;
    public readonly bool WeedKillerDeployed = weedKillerDeployed;
    public readonly bool HasHiveQueen = hasHiveQueen;
    public readonly bool T3Unlocked = t3Unlocked;
}

[Serializable, NetSerializable]
public sealed class XenoEvolveBuiMsg(EntProtoId choice) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Choice = choice;
}

[Serializable, NetSerializable]
public sealed class XenoStrainBuiMsg(EntProtoId choice) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Choice = choice;
}

[Serializable, NetSerializable]
public sealed class XenoT3RaffleEntryMsg(EntProtoId? choice) : BoundUserInterfaceMessage
{
    public readonly EntProtoId? Choice = choice;
}
