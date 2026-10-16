using Content.Shared.Explosion;

namespace Content.Shared._RMC14.Explosion;

public static class RMCExplosionVisuals
{
    public static float EpicenterPower(IReadOnlyList<float> intensity, float powerPerIntensity)
    {
        return intensity.Count == 0 ? 0 : intensity[0] * powerPerIntensity;
    }

    public static double SmokeLifetime(RMCExplosionSmokeVisuals smoke, float power)
    {
        var extra = Math.Clamp((power - smoke.ExtraLingerMinPower) / (smoke.ExtraLingerFullPower - smoke.ExtraLingerMinPower), 0, 1);
        return smoke.RingDelay.TotalSeconds + smoke.Linger.TotalSeconds + smoke.ExtraLinger.TotalSeconds * extra;
    }

    public static double SmokeEnd(ExplosionPrototype type, IReadOnlyList<float> intensity)
    {
        var smoke = type.Smoke;
        var end = 0.0;
        for (var ring = 0; ring < intensity.Count; ring++)
        {
            var ringEnd = ring * smoke.RingDelay.TotalSeconds +
                          smoke.MaxStartDelay.TotalSeconds +
                          SmokeLifetime(smoke, intensity[ring] * type.PowerPerIntensity);
            end = Math.Max(end, ringEnd);
        }

        return end;
    }

    public static double ShockWaveDuration(RMCExplosionShockWaveVisuals shockWave, float power)
    {
        return power / shockWave.PowerPerRadius * shockWave.DurationPerRadius.TotalSeconds;
    }

    public static double VisualLifetime(ExplosionPrototype type, IReadOnlyList<float> intensity)
    {
        if (type.PowerPerIntensity <= 0 || intensity.Count == 0)
            return 0;

        var lifetime = SmokeEnd(type, intensity);

        var power = EpicenterPower(intensity, type.PowerPerIntensity);
        if (power >= type.ShockWave.MinPower)
            lifetime = Math.Max(lifetime, ShockWaveDuration(type.ShockWave, power));

        return lifetime;
    }
}
