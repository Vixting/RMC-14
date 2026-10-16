namespace Content.Shared._RMC14.Explosion;

[DataDefinition]
public sealed partial class RMCExplosionSparkVisuals
{
    [DataField]
    public float MinPower = 300;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(0.15);

    [DataField]
    public TimeSpan FrameDelay = TimeSpan.FromSeconds(0.05);

    [DataField]
    public float PowerPerStreak = 25;

    [DataField]
    public int MinStreaks = 12;

    [DataField]
    public int MaxStreaks = 24;

    [DataField]
    public float PowerPerTile = 100;

    [DataField]
    public float MinReach = 2;

    [DataField]
    public float MaxReach = 6;

    [DataField]
    public float Length = 1;

    [DataField]
    public float Width = 0.35f;
}
