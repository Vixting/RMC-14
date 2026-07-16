using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Xenonids.Biomass;

namespace Content.Client._RMC14.Xenonids.Biomass;

public sealed class RMCBiomassAnalyzerUiRefreshSystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBiomassAnalyzerComponent, AfterAutoHandleStateEvent>(OnAnalyzerState);
    }

    private void OnAnalyzerState(Entity<RMCBiomassAnalyzerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var machines = EntityQueryEnumerator<RMCBiomassAnalyzerMachineComponent>();
        while (machines.MoveNext(out var uid, out _))
        {
            _rmcUI.RefreshUIs<RMCBiomassAnalyzerBui>(uid);
        }
    }
}
