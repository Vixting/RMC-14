using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sprite;

/// <summary>
/// Lowers this entitys sprite from <see cref="StartOffset"/> to its resting position over <see cref="Duration"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSpriteDescentSystem))]
public sealed partial class RMCSpriteDescentComponent : Component
{
    [DataField]
    public Vector2 StartOffset;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan StartTime;
}
