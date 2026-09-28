using Content.Client.Explosion;
using Content.Shared._RMC14.Explosion;
using Content.Shared.Explosion.Components;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Explosion;

public sealed class RMCExplosionLightSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float PowerPerEnergy = 100;
    private const float MinEnergy = 1;
    private const float MaxEnergy = 8;
    private const float FlashMultiplier = 1.5f;
    private const double FlashSeconds = 0.1;
    private const float StartRadiusMultiplier = 0.7f;

    public override void FrameUpdate(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<ExplosionVisualsComponent, ExplosionVisualsTexturesComponent>();
        while (query.MoveNext(out var visuals, out var textures))
        {
            if (textures.RMCPowerPerIntensity <= 0 ||
                visuals.Intensity.Count == 0 ||
                !_lights.TryGetLight(textures.LightEntity, out var light))
            {
                continue;
            }

            var power = RMCExplosionVisuals.EpicenterPower(visuals.Intensity, textures.RMCPowerPerIntensity);
            var peak = Math.Clamp(power / PowerPerEnergy, MinEnergy, MaxEnergy);
            var elapsed = (time - textures.RMCStartTime).TotalSeconds;
            var fadeEnd = RMCExplosionVisuals.SmokeEnd(visuals.Intensity, textures.RMCPowerPerIntensity);

            var flash = 1 + (FlashMultiplier - 1) * (float) Math.Max(0, 1 - elapsed / FlashSeconds);
            var fade = 1 - SmoothStep(FlashSeconds, fadeEnd, elapsed);
            var grow = StartRadiusMultiplier + (1 - StartRadiusMultiplier) * (float) Math.Min(elapsed / FlashSeconds, 1);

            _lights.SetEnergy(textures.LightEntity, peak * flash * fade, light);
            _lights.SetRadius(textures.LightEntity, visuals.Intensity.Count * grow, light);
        }
    }

    private static float SmoothStep(double from, double to, double value)
    {
        if (to <= from)
            return value >= to ? 1 : 0;

        var x = Math.Clamp((value - from) / (to - from), 0, 1);
        return (float) (x * x * (3 - 2 * x));
    }
}
