using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCPlantTraitScreamComponent : RMCPlantTraitComponent
{
    [DataField]
    public SoundSpecifier ScreamSound = new SoundCollectionSpecifier("PlantScreams", AudioParams.Default.WithVolume(-10));

    public override string? TraitState { get; set; } = "mutation-plant-scream";
}
