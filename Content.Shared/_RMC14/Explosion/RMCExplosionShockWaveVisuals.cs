namespace Content.Shared._RMC14.Explosion;

[DataDefinition]
public sealed partial class RMCExplosionShockWaveVisuals
{
    [DataField]
    public float MinPower = 150;

    [DataField]
    public float PowerPerRadius = 60;

    [DataField]
    public TimeSpan DurationPerRadius = TimeSpan.FromSeconds(0.05);

    [DataField]
    public float StartRadius = 0.5f;

    [DataField]
    public float EndRadiusMultiplier = 0.75f;

    [DataField]
    public float FalloffPower = 40;

    [DataField]
    public float Sharpness = 10;

    [DataField]
    public float Width = 0.8f;

    [DataField]
    public float DampStartPower = 300;

    [DataField]
    public float DampFullPower = 1000;

    [DataField]
    public float MinStrength = 0.35f;
}
