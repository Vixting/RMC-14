using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[NetworkedComponent]
public abstract partial class RMCPlantTraitComponent : Component
{
    [DataField]
    public virtual string? TraitState { get; set; }
}
