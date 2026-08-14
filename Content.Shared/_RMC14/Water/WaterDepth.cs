namespace Content.Shared._RMC14.Water;

/// <summary>
/// How submerged a mob standing in a given water entity should look. Ordinal order matters:
/// when a mob is touching more than one water entity at once, <see cref="RMCWaterOverlaySystem"/>
/// picks the highest ordinal (most submerged) as the effective depth.
/// </summary>
public enum WaterDepth : byte
{
    CoastShallow,
    CoastDeep,
    Shallow,
    Intermediate,
    Deep,
}

public static class WaterDepthOffsets
{
    public static float GetSpriteOffset(WaterDepth depth)
    {
        return depth switch
        {
            WaterDepth.CoastShallow => -2f / 32f,
            WaterDepth.CoastDeep => -4f / 32f,
            WaterDepth.Shallow => -8f / 32f,
            WaterDepth.Intermediate => -12f / 32f,
            WaterDepth.Deep => -18f / 32f,
            _ => 0f,
        };
    }
}
