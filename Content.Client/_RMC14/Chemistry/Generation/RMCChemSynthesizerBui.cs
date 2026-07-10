using Content.Shared._RMC14.Chemistry.Generation;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Chemistry.Generation;

[UsedImplicitly]
public sealed class RMCChemSynthesizerBui : BoundUserInterface
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private RMCChemSynthesizerWindow? _window;

    public RMCChemSynthesizerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RMCChemSynthesizerWindow>();
        _window.SetBui(this);
        Refresh();
    }

    public void SetTier(int tier)
    {
        SendPredictedMessage(new RMCChemSynthesizerSetTierBuiMsg(tier));
    }

    public void Synthesize()
    {
        SendPredictedMessage(new RMCChemSynthesizerSynthesizeBuiMsg());
    }

    public override void Update()
    {
        base.Update();
        Refresh();
    }

    private void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCChemSynthesizerComponent? comp))
            return;

        _window.UpdateState(comp, _timing.CurTime);
    }
}
