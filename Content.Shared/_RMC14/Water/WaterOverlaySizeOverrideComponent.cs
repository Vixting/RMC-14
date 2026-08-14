namespace Content.Shared._RMC14.Water;

[RegisterComponent]
public sealed partial class WaterOverlaySizeOverrideComponent : Component
{
    [DataField(required: true)]
    public int Size;
}
