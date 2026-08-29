using Content.Shared._RMC14.Botany;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;

namespace Content.Client._RMC14.Botany;

[UsedImplicitly]
public sealed class RMCPlantAnalyzerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private RMCPlantAnalyzerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RMCPlantAnalyzerWindow>();
        _window.Title = "Plant Analyzer";

        if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out RMCPlantAnalyzerComponent? comp))
            _window.UpdateState(comp);
    }

    public void Refresh(RMCPlantAnalyzerComponent comp)
    {
        _window?.UpdateState(comp);
    }
}
