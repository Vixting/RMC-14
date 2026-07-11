using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.UserInterface;

namespace Content.Client._RMC14.Chemistry.Generation;

public sealed class RMCSynthesisSimulatorSystem : SharedRMCSynthesisSimulatorSystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    protected override void RefreshUIs(Entity<RMCSynthesisSimulatorComponent> ent)
    {
        _rmcUI.RefreshUIs<RMCSynthesisSimulatorBui>(ent.Owner);
    }
}
