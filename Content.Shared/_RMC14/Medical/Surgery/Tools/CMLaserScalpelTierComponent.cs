using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Surgery.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMLaserScalpelTierComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Tier = 1;
}
