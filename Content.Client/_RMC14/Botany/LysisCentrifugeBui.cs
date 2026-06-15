using Content.Shared._RMC14.Botany;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;

namespace Content.Client._RMC14.Botany;

[UsedImplicitly]
public sealed class LysisCentrifugeBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private LysisCentrifugeWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LysisCentrifugeWindow>();
        _window.Title = Loc.GetString("rmc-centrifuge-window-title");
        _window.SetBui(this);

        if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out LysisCentrifugeComponent? comp))
            _window.UpdateState(comp);
    }

    public void Refresh(LysisCentrifugeComponent comp)
    {
        _window?.UpdateState(comp);
    }

    public void ExtractGene(PlantGeneType geneType)
    {
        SendMessage(new LysisCentrifugeExtractGeneBuiMsg(geneType));
    }

    public void ProcessSeed()
    {
        SendMessage(new LysisCentrifugeProcessSeedBuiMsg());
    }

    public void EjectSeed()
    {
        SendMessage(new LysisCentrifugeEjectSeedBuiMsg());
    }

    public void ClearBuffer()
    {
        SendMessage(new LysisCentrifugeClearBufferBuiMsg());
    }

    public void EjectDisc()
    {
        SendMessage(new LysisCentrifugeEjectDiscBuiMsg());
    }
}
