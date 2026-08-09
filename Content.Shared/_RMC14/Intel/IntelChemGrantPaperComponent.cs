using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelChemGrantPaperComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Grant;
}
