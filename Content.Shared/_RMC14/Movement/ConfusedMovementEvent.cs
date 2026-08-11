using System.Numerics;

namespace Content.Shared._RMC14.Movement;

[ByRefEvent]
public struct ConfusedMovementEvent
{
    public Vector2 WishDir;

    public ConfusedMovementEvent(Vector2 wishDir)
    {
        WishDir = wishDir;
    }
}
