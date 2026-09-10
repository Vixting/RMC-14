using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPlantTraitCarnivorousComponent : RMCPlantTraitComponent
{
    [DataField, AutoNetworkedField]
    public int Level = 1;
}
