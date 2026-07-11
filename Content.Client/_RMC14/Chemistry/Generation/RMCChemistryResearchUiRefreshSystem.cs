using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.UserInterface;

namespace Content.Client._RMC14.Chemistry.Generation;

public sealed class RMCChemistryResearchUiRefreshSystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCChemistryResearchComponent, AfterAutoHandleStateEvent>(OnResearchState);
    }

    private void OnResearchState(Entity<RMCChemistryResearchComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var computers = EntityQueryEnumerator<RMCResearchComputerComponent>();
        while (computers.MoveNext(out var uid, out _))
        {
            _rmcUI.RefreshUIs<RMCResearchComputerBui>(uid);
        }

        var simulators = EntityQueryEnumerator<RMCSynthesisSimulatorComponent>();
        while (simulators.MoveNext(out var uid, out _))
        {
            _rmcUI.RefreshUIs<RMCSynthesisSimulatorBui>(uid);
        }
    }
}
