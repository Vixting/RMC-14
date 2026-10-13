namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Blood color used by blood particles, for mobs whose blood isnt a bloodstream reagent.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleBloodColorComponent : Component
{
    [DataField(required: true)]
    public Color Color;

    [DataField]
    public bool SplatterOnHit = true;
}
