using Content.Shared._RMC14.Fishing;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Fishing;

[UsedImplicitly]
public sealed class RMCFishingMinigameBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private RMCFishingMinigameWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCFishingMinigameWindow>();
        _window.OnResult += OnResult;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not RMCFishingMinigameBuiState cast)
            return;

        _window?.StartMinigame(cast.Difficulty, cast.Token);
    }

    private void OnResult(bool success, int token)
    {
        SendMessage(new RMCFishingMinigameResultMsg(success, token));
    }
}
