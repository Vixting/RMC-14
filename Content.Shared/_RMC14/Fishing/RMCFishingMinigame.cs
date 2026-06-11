using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Fishing;

[Serializable, NetSerializable]
public enum RMCFishingMinigameUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCFishingMinigameBuiState : BoundUserInterfaceState
{
    public readonly float Difficulty;
    public readonly int Token;

    public RMCFishingMinigameBuiState(float difficulty, int token)
    {
        Difficulty = difficulty;
        Token = token;
    }
}

[Serializable, NetSerializable]
public sealed class RMCFishingMinigameResultMsg : BoundUserInterfaceMessage
{
    public readonly bool Success;
    public readonly int Token;

    public RMCFishingMinigameResultMsg(bool success, int token)
    {
        Success = success;
        Token = token;
    }
}
