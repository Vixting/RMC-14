namespace Content.Shared._RMC14.Explosion;

[DataDefinition]
public sealed partial class RMCExplosionScorch
{
    [DataField]
    public string HeavyDecalTag = "RMCScorch";

    [DataField]
    public string LightDecalTag = "RMCScorchSmall";

    [DataField]
    public float HeavyPower = 200;

    [DataField]
    public float LightPower = 100;

    [DataField]
    public int CoreRings = 1;

    [DataField]
    public float NoiseFrequency = 0.18f;

    [DataField]
    public float FullCoveragePower = 600;

    [DataField]
    public float MinCoverage = 0.35f;

    [DataField]
    public float MaxCoverage = 0.85f;

    [DataField]
    public float Jitter = 0.25f;

    [DataField]
    public float MinAlpha = 0.6f;

    [DataField]
    public float MinBrightness = 0.8f;

    [DataField]
    public TimeSpan RingDelay = TimeSpan.FromSeconds(0.015);
}
