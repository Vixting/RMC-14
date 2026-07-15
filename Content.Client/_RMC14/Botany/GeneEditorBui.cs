using Content.Shared._RMC14.Botany;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;

namespace Content.Client._RMC14.Botany;

[UsedImplicitly]
public sealed class GeneEditorBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private GeneEditorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GeneEditorWindow>();
        _window.Title = "Bioballistic Delivery System";
        _window.SetBui(this);

        if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out GeneEditorComponent? comp))
            _window.UpdateState(comp);
    }

    public void Refresh(GeneEditorComponent comp)
    {
        _window?.UpdateState(comp);
    }

    public void Apply()
    {
        SendMessage(new GeneEditorApplyBuiMsg());
    }

    public void Eject()
    {
        SendMessage(new GeneEditorEjectBuiMsg());
    }
}
