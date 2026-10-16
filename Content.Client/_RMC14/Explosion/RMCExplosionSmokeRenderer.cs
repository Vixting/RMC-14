using System.Numerics;
using Content.Client.Explosion;
using Content.Shared._RMC14.Explosion;
using Content.Shared.Explosion.Components;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Explosion;

public static class RMCExplosionSmokeRenderer
{
    public static void DrawSmokeWave(
        DrawingHandleWorld drawHandle,
        Box2 gridBounds,
        Dictionary<int, List<Vector2i>> tileSets,
        ExplosionVisualsComponent visuals,
        ushort tileSize,
        ExplosionVisualsTexturesComponent textures,
        Vector2 epicenter,
        TimeSpan curTime)
    {
        if (textures.RMCType is not { } type || textures.RMCSmokeFrames.Count == 0)
            return;

        var smoke = type.Smoke;
        var ringSeconds = smoke.RingDelay.TotalSeconds;
        var maxStartDelay = smoke.MaxStartDelay.TotalSeconds;
        var maxLifetime = RMCExplosionVisuals.SmokeLifetime(smoke, float.MaxValue);
        var elapsed = (curTime - textures.RMCStartTime).TotalSeconds;
        var newestRing = (int) (elapsed / ringSeconds);
        var oldestRing = (int) Math.Ceiling((elapsed - maxLifetime - maxStartDelay) / ringSeconds);
        var baseColor = textures.FireColor ?? Color.White;

        for (var ring = Math.Max(0, oldestRing); ring <= Math.Min(newestRing, visuals.Intensity.Count - 1); ring++)
        {
            if (!tileSets.TryGetValue(ring, out var tiles))
                continue;

            var power = visuals.Intensity[ring] * textures.RMCPowerPerIntensity;
            var lifetime = RMCExplosionVisuals.SmokeLifetime(smoke, power);
            var ringAlpha = ring == 0 ? 1f : Math.Clamp(power / 255f, 0f, 1f);
            var heat = Math.Clamp((power - smoke.HeatMinPower) / (smoke.HeatFullPower - smoke.HeatMinPower), 0f, 1f);
            var core = power >= smoke.CorePower && textures.RMCCoreFrames.Count > 0;

            foreach (var tile in tiles)
            {
                var centre = (tile + Vector2Helpers.Half) * tileSize;
                if (!gridBounds.Contains(centre))
                    continue;

                var hash = HashTile(tile, textures.RMCSeed);
                var start = ring * ringSeconds + Hash01(hash, 1) * maxStartDelay;
                var t = (float) ((elapsed - start) / lifetime);
                if (t is < 0 or >= 1)
                    continue;

                var eased = 1 - (1 - t) * (1 - t);
                var fadeIn = Math.Min(t / smoke.FadeInFraction, 1f);
                var fadeOut = 1 - SmoothStep(smoke.FadeOutStart, 1f, t);
                var alpha = ringAlpha * fadeIn * fadeOut;

                var away = centre - epicenter;
                var drift = away.LengthSquared() > 0.0001f ? Vector2.Normalize(away) * smoke.Drift * tileSize * eased : Vector2.Zero;
                var jitter = new Vector2(Hash01(hash, 2) - 0.5f, Hash01(hash, 3) - 0.5f) * smoke.Jitter * tileSize;
                var position = centre + jitter + drift;
                var rotation = Angle.FromDegrees(Hash01(hash, 4) * 360);

                var tint = Color.InterpolateBetween(Color.White, textures.RMCHeatColor, heat * (1 - eased));
                var color = new Color(baseColor.R * tint.R, baseColor.G * tint.G, baseColor.B * tint.B, baseColor.A * alpha);

                var scale = smoke.Overlap * (smoke.StartScale + (smoke.EndScale - smoke.StartScale) * eased);
                var frames = textures.RMCSmokeFrames;
                if (core && Hash01(hash, 5) < smoke.CoreChance)
                {
                    frames = textures.RMCCoreFrames;
                    scale *= smoke.CoreScale;
                    color = color.WithAlpha(color.A * smoke.CoreAlpha);
                }

                var texture = frames[(int) (Hash01(hash, 6) * frames.Count) % frames.Count];
                var size = tileSize * scale;
                var quad = Box2.CenteredAround(position, new Vector2(size, size));
                drawHandle.DrawTextureRect(texture, new Box2Rotated(quad, rotation, quad.Center), color);
            }
        }
    }

    public static void DrawSparkStreaks(
        DrawingHandleWorld drawHandle,
        ExplosionVisualsComponent visuals,
        ExplosionVisualsTexturesComponent textures,
        TimeSpan curTime)
    {
        if (textures.RMCType is not { } type ||
            textures.RMCPowerPerIntensity <= 0 ||
            textures.RMCSparkFrames.Count == 0)
        {
            return;
        }

        var sparks = type.Sparks;
        var power = RMCExplosionVisuals.EpicenterPower(visuals.Intensity, textures.RMCPowerPerIntensity);
        if (power < sparks.MinPower)
            return;

        var elapsed = (curTime - textures.RMCStartTime).TotalSeconds;
        var t = (float) (elapsed / sparks.Duration.TotalSeconds);
        if (t is < 0 or >= 1)
            return;

        drawHandle.SetTransform(Matrix3x2.Identity);

        var count = Math.Clamp((int) (power / sparks.PowerPerStreak), sparks.MinStreaks, sparks.MaxStreaks);
        var reach = Math.Clamp(power / sparks.PowerPerTile, sparks.MinReach, sparks.MaxReach);
        var eased = 1 - (1 - t) * (1 - t);
        var tint = Color.InterpolateBetween(textures.RMCHeatColor, Color.White, t);
        var color = tint.WithAlpha(1 - t);

        for (var i = 0; i < count; i++)
        {
            var hash = HashTile(new Vector2i(i, 0), textures.RMCSeed);
            var angle = Angle.FromDegrees(360.0 * i / count + (Hash01(hash, 1) - 0.5f) * 360.0 / count);
            var direction = angle.ToVec();
            var distance = reach * eased * (0.6f + 0.4f * Hash01(hash, 2));
            var position = visuals.Epicenter.Position + direction * distance;

            var frames = textures.RMCSparkFrames[(int) (Hash01(hash, 3) * textures.RMCSparkFrames.Count) % textures.RMCSparkFrames.Count];
            if (frames.Length == 0)
                continue;

            var texture = frames[(int) (elapsed / sparks.FrameDelay.TotalSeconds) % frames.Length];
            var quad = Box2.CenteredAround(position, new Vector2(sparks.Length, sparks.Width));
            drawHandle.DrawTextureRect(texture, new Box2Rotated(quad, angle, quad.Center), color);
        }
    }

    private static float SmoothStep(float from, float to, float value)
    {
        var x = Math.Clamp((value - from) / (to - from), 0f, 1f);
        return x * x * (3 - 2 * x);
    }

    private static int HashTile(Vector2i tile, int seed)
    {
        unchecked
        {
            var hash = tile.X * 73856093 ^ tile.Y * 19349663 ^ seed * 83492791;
            hash ^= hash >> 13;
            hash *= 0x5bd1e995;
            hash ^= hash >> 15;
            return hash;
        }
    }

    private static float Hash01(int hash, int salt)
    {
        unchecked
        {
            var mixed = (uint) (hash + salt * 0x27d4eb2d);
            mixed ^= mixed >> 15;
            mixed *= 0x2c1b3c6dU;
            mixed ^= mixed >> 12;
            mixed *= 0x297a2d39U;
            mixed ^= mixed >> 15;
            return (mixed & 0xFFFFFF) / 16777216f;
        }
    }
}
