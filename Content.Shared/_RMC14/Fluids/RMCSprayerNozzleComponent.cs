using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Fluids;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSprayerNozzleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Backpack;

    [DataField, AutoNetworkedField]
    public FixedPoint2 RefillAmount = FixedPoint2.New(10);
}
