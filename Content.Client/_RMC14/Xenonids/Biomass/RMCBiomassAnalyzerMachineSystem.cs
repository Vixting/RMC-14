using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Xenonids.Biomass;

namespace Content.Client._RMC14.Xenonids.Biomass;

public sealed class RMCBiomassAnalyzerMachineSystem : SharedRMCBiomassAnalyzerSystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    protected override void RefreshUIs(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        _rmcUI.RefreshUIs<RMCBiomassAnalyzerBui>(ent.Owner);
    }
}
