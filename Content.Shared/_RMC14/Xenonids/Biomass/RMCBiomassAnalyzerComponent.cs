using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Biomass;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCBiomassAnalyzerComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Points;

    [DataField, AutoNetworkedField]
    public Dictionary<string, int> PurchaseCounts = new();
}
