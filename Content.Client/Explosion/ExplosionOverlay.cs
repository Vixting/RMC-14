using System.Numerics;
using Content.Shared._RMC14.Explosion; // RMC14
using Content.Shared.Explosion.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing; // RMC14

namespace Content.Client.Explosion;

[UsedImplicitly]
public sealed class ExplosionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!; // RMC14

    // RMC14
    private const double RMCSmokeRingSeconds = RMCExplosionVisuals.RingSeconds;
    private const double RMCSmokeMaxStartDelay = RMCExplosionVisuals.SmokeMaxStartDelay;
    private const float RMCSmokeFadeInFraction = 0.15f;
    private const float RMCSmokeFadeOutStart = 0.6f;
    private const float RMCSmokeOverlap = 1.4f;
    private const float RMCSmokeStartScale = 0.9f;
    private const float RMCSmokeEndScale = 1.4f;
    private const float RMCSmokeJitter = 0.4f;
    private const float RMCSmokeDrift = 0.35f;
    private const float RMCSmokeHeatMinPower = 200;
    private const float RMCSmokeHeatFullPower = 600;
    private const float RMCSmokeCorePower = 400;
    private const float RMCSmokeCoreChance = 0.35f;
    private const float RMCSmokeCoreScale = 1.8f;
    private const float RMCSmokeCoreAlpha = 0.7f;
    private const float RMCSparkPowerPerStreak = 25;
    private const int RMCSparkMinStreaks = 12;
    private const int RMCSparkMaxStreaks = 24;
    private const float RMCSparkPowerPerTile = 100;
    private const float RMCSparkMinReach = 2;
    private const float RMCSparkMaxReach = 6;
    private const float RMCSparkLength = 1f;
    private const float RMCSparkWidth = 0.35f;
    private const double RMCSparkFrameSeconds = 0.05;
    // RMC14
    private readonly SharedTransformSystem _transformSystem;
    private SharedAppearanceSystem _appearance;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private ShaderInstance _shader;

    public ExplosionOverlay(SharedAppearanceSystem appearanceSystem)
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(UnshadedShader).Instance();
        _transformSystem = _entMan.System<SharedTransformSystem>();
        _appearance = appearanceSystem;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var drawHandle = args.WorldHandle;
        drawHandle.UseShader(_shader);

        var xforms = _entMan.GetEntityQuery<TransformComponent>();
        var query = _entMan.EntityQueryEnumerator<ExplosionVisualsComponent, ExplosionVisualsTexturesComponent>();

        while (query.MoveNext(out var uid, out var visuals, out var textures))
        {
            if (visuals.Epicenter.MapId != args.MapId)
                continue;

            if (!_appearance.TryGetData(uid, ExplosionAppearanceData.Progress, out int index))
                continue;

            index = Math.Min(index, visuals.Intensity.Count - 1);
            DrawExplosion(drawHandle, args.WorldBounds, visuals, index, xforms, textures);
            DrawRMCSparkStreaks(drawHandle, visuals, textures); // RMC14
        }

        drawHandle.SetTransform(Matrix3x2.Identity);
        drawHandle.UseShader(null);
    }

    private void DrawExplosion(
        DrawingHandleWorld drawHandle,
        Box2Rotated worldBounds,
        ExplosionVisualsComponent visuals,
        int index,
        EntityQuery<TransformComponent> xforms,
        ExplosionVisualsTexturesComponent textures)
    {
        Box2 gridBounds;
        foreach (var (gridId, tiles) in visuals.Tiles)
        {
            if (!_entMan.TryGetComponent(gridId, out MapGridComponent? grid))
                continue;

            var xform = xforms.GetComponent(gridId);
            var (_, _, worldMatrix, invWorldMatrix) = _transformSystem.GetWorldPositionRotationMatrixWithInv(xform, xforms);

            gridBounds = invWorldMatrix.TransformBox(worldBounds).Enlarged(grid.TileSize * 2);
            drawHandle.SetTransform(worldMatrix);

            DrawTiles(drawHandle, gridBounds, index, tiles, visuals, grid.TileSize, textures, Vector2.Transform(visuals.Epicenter.Position, invWorldMatrix)); // RMC14
        }

        if (visuals.SpaceTiles == null)
            return;

        Matrix3x2.Invert(visuals.SpaceMatrix, out var invSpace);
        gridBounds = invSpace.TransformBox(worldBounds).Enlarged(2);
        drawHandle.SetTransform(visuals.SpaceMatrix);

        DrawTiles(drawHandle, gridBounds, index, visuals.SpaceTiles, visuals, visuals.SpaceTileSize, textures, Vector2.Transform(visuals.Epicenter.Position, invSpace)); // RMC14
    }

    private void DrawTiles(
        DrawingHandleWorld drawHandle,
        Box2 gridBounds,
        int index,
        Dictionary<int, List<Vector2i>> tileSets,
        ExplosionVisualsComponent visuals,
        ushort tileSize,
        ExplosionVisualsTexturesComponent textures,
        Vector2 rmcEpicenter = default) // RMC14
    {
        // RMC14
        if (textures.RMCPowerPerIntensity > 0)
        {
            DrawRMCSmokeWave(drawHandle, gridBounds, tileSets, visuals, tileSize, textures, rmcEpicenter);
            return;
        }
        // RMC14

        for (var j = 0; j <= index; j++)
        {
            if (!tileSets.TryGetValue(j, out var tiles))
                continue;

            var frameIndex = (int) Math.Min(visuals.Intensity[j] / textures.IntensityPerState, textures.FireFrames.Count - 1);
            var frames = textures.FireFrames[frameIndex];

            foreach (var tile in tiles)
            {
                var centre = (tile + Vector2Helpers.Half) * tileSize;

                if (!gridBounds.Contains(centre))
                    continue;

                var texture = _robustRandom.Pick(frames);
                drawHandle.DrawTextureRect(texture, Box2.CenteredAround(centre, new Vector2(tileSize, tileSize)), textures.FireColor);
            }
        }
    }

    // RMC14
    private void DrawRMCSmokeWave(
        DrawingHandleWorld drawHandle,
        Box2 gridBounds,
        Dictionary<int, List<Vector2i>> tileSets,
        ExplosionVisualsComponent visuals,
        ushort tileSize,
        ExplosionVisualsTexturesComponent textures,
        Vector2 epicenter)
    {
        if (textures.RMCSmokeFrames.Count == 0)
            return;

        var maxLifetime = RMCExplosionVisuals.SmokeLifetime(float.MaxValue);
        var elapsed = (_timing.CurTime - textures.RMCStartTime).TotalSeconds;
        var newestRing = (int) (elapsed / RMCSmokeRingSeconds);
        var oldestRing = (int) Math.Ceiling((elapsed - maxLifetime - RMCSmokeMaxStartDelay) / RMCSmokeRingSeconds);
        var baseColor = textures.FireColor ?? Color.White;

        for (var ring = Math.Max(0, oldestRing); ring <= Math.Min(newestRing, visuals.Intensity.Count - 1); ring++)
        {
            if (!tileSets.TryGetValue(ring, out var tiles))
                continue;

            var power = visuals.Intensity[ring] * textures.RMCPowerPerIntensity;
            var lifetime = RMCExplosionVisuals.SmokeLifetime(power);
            var ringAlpha = ring == 0 ? 1f : Math.Clamp(power / 255f, 0f, 1f);
            var heat = Math.Clamp((power - RMCSmokeHeatMinPower) / (RMCSmokeHeatFullPower - RMCSmokeHeatMinPower), 0f, 1f);
            var core = power >= RMCSmokeCorePower && textures.RMCCoreFrames.Count > 0;

            foreach (var tile in tiles)
            {
                var centre = (tile + Vector2Helpers.Half) * tileSize;
                if (!gridBounds.Contains(centre))
                    continue;

                var hash = RMCHashTile(tile, textures.RMCSeed);
                var start = ring * RMCSmokeRingSeconds + RMCHash01(hash, 1) * RMCSmokeMaxStartDelay;
                var t = (float) ((elapsed - start) / lifetime);
                if (t is < 0 or >= 1)
                    continue;

                var eased = 1 - (1 - t) * (1 - t);
                var fadeIn = Math.Min(t / RMCSmokeFadeInFraction, 1f);
                var fadeOut = 1 - SmoothStep(RMCSmokeFadeOutStart, 1f, t);
                var alpha = ringAlpha * fadeIn * fadeOut;

                var away = centre - epicenter;
                var drift = away.LengthSquared() > 0.0001f ? Vector2.Normalize(away) * RMCSmokeDrift * tileSize * eased : Vector2.Zero;
                var jitter = new Vector2(RMCHash01(hash, 2) - 0.5f, RMCHash01(hash, 3) - 0.5f) * RMCSmokeJitter * tileSize;
                var position = centre + jitter + drift;
                var rotation = Angle.FromDegrees(RMCHash01(hash, 4) * 360);

                var tint = Color.InterpolateBetween(Color.White, textures.RMCHeatColor, heat * (1 - eased));
                var color = new Color(baseColor.R * tint.R, baseColor.G * tint.G, baseColor.B * tint.B, baseColor.A * alpha);

                var scale = RMCSmokeOverlap * (RMCSmokeStartScale + (RMCSmokeEndScale - RMCSmokeStartScale) * eased);
                var frames = textures.RMCSmokeFrames;
                if (core && RMCHash01(hash, 5) < RMCSmokeCoreChance)
                {
                    frames = textures.RMCCoreFrames;
                    scale *= RMCSmokeCoreScale;
                    color = color.WithAlpha(color.A * RMCSmokeCoreAlpha);
                }

                var texture = frames[(int) (RMCHash01(hash, 6) * frames.Count) % frames.Count];
                var size = tileSize * scale;
                var quad = Box2.CenteredAround(position, new Vector2(size, size));
                drawHandle.DrawTextureRect(texture, new Box2Rotated(quad, rotation, quad.Center), color);
            }
        }
    }

    private void DrawRMCSparkStreaks(DrawingHandleWorld drawHandle, ExplosionVisualsComponent visuals, ExplosionVisualsTexturesComponent textures)
    {
        if (textures.RMCPowerPerIntensity <= 0 || textures.RMCSparkFrames.Count == 0)
            return;

        var power = RMCExplosionVisuals.EpicenterPower(visuals.Intensity, textures.RMCPowerPerIntensity);
        if (power < RMCExplosionVisuals.SparkStreakMinPower)
            return;

        var elapsed = (_timing.CurTime - textures.RMCStartTime).TotalSeconds;
        var t = (float) (elapsed / RMCExplosionVisuals.SparkStreakSeconds);
        if (t is < 0 or >= 1)
            return;

        drawHandle.SetTransform(Matrix3x2.Identity);

        var count = Math.Clamp((int) (power / RMCSparkPowerPerStreak), RMCSparkMinStreaks, RMCSparkMaxStreaks);
        var reach = Math.Clamp(power / RMCSparkPowerPerTile, RMCSparkMinReach, RMCSparkMaxReach);
        var eased = 1 - (1 - t) * (1 - t);
        var tint = Color.InterpolateBetween(textures.RMCHeatColor, Color.White, t);
        var color = tint.WithAlpha(1 - t);

        for (var i = 0; i < count; i++)
        {
            var hash = RMCHashTile(new Vector2i(i, 0), textures.RMCSeed);
            var angle = Angle.FromDegrees(360.0 * i / count + (RMCHash01(hash, 1) - 0.5f) * 360.0 / count);
            var direction = angle.ToVec();
            var distance = reach * eased * (0.6f + 0.4f * RMCHash01(hash, 2));
            var position = visuals.Epicenter.Position + direction * distance;

            var frames = textures.RMCSparkFrames[(int) (RMCHash01(hash, 3) * textures.RMCSparkFrames.Count) % textures.RMCSparkFrames.Count];
            if (frames.Length == 0)
                continue;

            var texture = frames[(int) (elapsed / RMCSparkFrameSeconds) % frames.Length];
            var quad = Box2.CenteredAround(position, new Vector2(RMCSparkLength, RMCSparkWidth));
            drawHandle.DrawTextureRect(texture, new Box2Rotated(quad, angle, quad.Center), color);
        }
    }

    private static float SmoothStep(float from, float to, float value)
    {
        var x = Math.Clamp((value - from) / (to - from), 0f, 1f);
        return x * x * (3 - 2 * x);
    }

    private static int RMCHashTile(Vector2i tile, int seed)
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

    private static float RMCHash01(int hash, int salt)
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
    // RMC14
}
