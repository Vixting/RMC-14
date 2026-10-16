namespace Content.Shared._RMC14.Explosion;

[DataDefinition]
public sealed partial class RMCExplosionSmokeVisuals
{
    [DataField]
    public TimeSpan RingDelay = TimeSpan.FromSeconds(0.05);

    [DataField]
    public TimeSpan MaxStartDelay = TimeSpan.FromSeconds(0.03);

    [DataField]
    public TimeSpan Linger = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan ExtraLinger = TimeSpan.FromSeconds(1);

    [DataField]
    public float ExtraLingerMinPower = 200;

    [DataField]
    public float ExtraLingerFullPower = 1000;

    [DataField]
    public float FadeInFraction = 0.15f;

    [DataField]
    public float FadeOutStart = 0.6f;

    [DataField]
    public float Overlap = 1.4f;

    [DataField]
    public float StartScale = 0.9f;

    [DataField]
    public float EndScale = 1.4f;

    [DataField]
    public float Jitter = 0.4f;

    [DataField]
    public float Drift = 0.35f;

    [DataField]
    public float HeatMinPower = 200;

    [DataField]
    public float HeatFullPower = 600;

    [DataField]
    public float CorePower = 400;

    [DataField]
    public float CoreChance = 0.35f;

    [DataField]
    public float CoreScale = 1.8f;

    [DataField]
    public float CoreAlpha = 0.7f;
}
