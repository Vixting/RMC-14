using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.UserInterface;

namespace Content.Client._RMC14.Chemistry.Generation;

public sealed class RMCResearchComputerSystem : SharedRMCResearchComputerSystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    protected override void RefreshUIs(Entity<RMCResearchComputerComponent> ent)
    {
        _rmcUI.RefreshUIs<RMCResearchComputerBui>(ent.Owner);
    }
}
