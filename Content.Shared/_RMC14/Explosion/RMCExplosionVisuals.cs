namespace Content.Shared._RMC14.Explosion;

public static class RMCExplosionVisuals
{
    public const double RingSeconds = 0.05;
    public const double SmokeLingerSeconds = 0.5;
    public const double SmokeExtraLingerSeconds = 1.0;
    public const float SmokeExtraLingerMinPower = 200;
    public const float SmokeExtraLingerFullPower = 1000;
    public const double SmokeMaxStartDelay = 0.03;

    public const float ShockWaveMinPower = 150;
    public const float ShockWavePowerPerRadius = 60;
    public const double ShockWaveSecondsPerRadius = 0.05;

    public const float BigSoundMinPower = 300;
    public const float SparkStreakMinPower = 300;
    public const double SparkStreakSeconds = 0.15;

    public static double SmokeLifetime(float power)
    {
        var extra = Math.Clamp((power - SmokeExtraLingerMinPower) / (SmokeExtraLingerFullPower - SmokeExtraLingerMinPower), 0, 1);
        return RingSeconds + SmokeLingerSeconds + SmokeExtraLingerSeconds * extra;
    }

    public static float EpicenterPower(IReadOnlyList<float> intensity, float powerPerIntensity)
    {
        return intensity.Count == 0 ? 0 : intensity[0] * powerPerIntensity;
    }

    public static double SmokeEnd(IReadOnlyList<float> intensity, float powerPerIntensity)
    {
        var end = 0.0;
        for (var ring = 0; ring < intensity.Count; ring++)
        {
            var ringEnd = ring * RingSeconds + SmokeMaxStartDelay + SmokeLifetime(intensity[ring] * powerPerIntensity);
            end = Math.Max(end, ringEnd);
        }

        return end;
    }

    public static double VisualLifetime(IReadOnlyList<float> intensity, float powerPerIntensity)
    {
        if (powerPerIntensity <= 0 || intensity.Count == 0)
            return 0;

        var lifetime = SmokeEnd(intensity, powerPerIntensity);

        var power = EpicenterPower(intensity, powerPerIntensity);
        if (power >= ShockWaveMinPower)
            lifetime = Math.Max(lifetime, power / ShockWavePowerPerRadius * ShockWaveSecondsPerRadius);

        return lifetime;
    }
}
