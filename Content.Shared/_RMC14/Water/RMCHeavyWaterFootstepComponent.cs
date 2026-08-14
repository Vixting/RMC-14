using Robust.Shared.Audio;

namespace Content.Shared._RMC14.Water;

[RegisterComponent]
[Access(typeof(RMCWaterOverlaySystem))]
public sealed partial class RMCHeavyWaterFootstepComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    public SoundSpecifier? PreviousSound;

    public bool HadFootstepModifier;
    public bool Swapped;
}
