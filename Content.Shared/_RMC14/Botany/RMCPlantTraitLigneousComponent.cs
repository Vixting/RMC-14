using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCPlantTraitLigneousComponent : RMCPlantTraitComponent
{
    public override string? TraitState { get; set; } = "mutation-plant-ligneous";
}
