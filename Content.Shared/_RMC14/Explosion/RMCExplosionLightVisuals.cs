namespace Content.Shared._RMC14.Explosion;

[DataDefinition]
public sealed partial class RMCExplosionLightVisuals
{
    [DataField]
    public float PowerPerEnergy = 100;

    [DataField]
    public float MinEnergy = 1;

    [DataField]
    public float MaxEnergy = 8;

    [DataField]
    public float FlashMultiplier = 1.5f;

    [DataField]
    public TimeSpan FlashDuration = TimeSpan.FromSeconds(0.1);

    [DataField]
    public float StartRadiusMultiplier = 0.7f;
}
