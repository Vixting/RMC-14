using System.Numerics;
using Content.Client.Explosion;
using Content.Shared._RMC14.Explosion;
using Content.Shared.Explosion.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Explosion;

public sealed class RMCExplosionShockWaveOverlay : Overlay, IEntityEventSubscriber
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<ShaderPrototype> ShockWaveShader = "RMCShockWave";

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;

    /// <summary>
    ///     Maximum number of distortions that can be shown on screen at a time.
    /// </summary>
    public const int MaxCount = 10;

    public RMCExplosionShockWaveOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(ShockWaveShader).Instance().Duplicate();
    }

    private readonly Vector2[] _positions = new Vector2[MaxCount];
    private readonly float[] _radii = new float[MaxCount];
    private readonly float[] _falloffPower = new float[MaxCount];
    private readonly float[] _sharpness = new float[MaxCount];
    private readonly float[] _width = new float[MaxCount];
    private readonly float[] _strength = new float[MaxCount];
    private int _count;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return false;

        _count = 0;
        var time = _timing.CurTime;

        var explosions = _entMan.EntityQueryEnumerator<ExplosionVisualsComponent, ExplosionVisualsTexturesComponent>();
        while (_count < MaxCount && explosions.MoveNext(out _, out var visuals, out var textures))
        {
            if (textures.RMCType is not { } type ||
                textures.RMCPowerPerIntensity <= 0 ||
                visuals.Intensity.Count == 0 ||
                visuals.Epicenter.MapId != args.MapId)
            {
                continue;
            }

            var shockWave = type.ShockWave;
            var power = visuals.Intensity[0] * textures.RMCPowerPerIntensity;
            if (power < shockWave.MinPower)
                continue;

            var radius = power / shockWave.PowerPerRadius;
            var duration = RMCExplosionVisuals.ShockWaveDuration(shockWave, power);
            var progress = (time - textures.RMCStartTime).TotalSeconds / duration;
            if (progress is < 0 or >= 1)
                continue;

            var ringRadius = shockWave.StartRadius + (radius * shockWave.EndRadiusMultiplier - shockWave.StartRadius) * (float) progress;
            var damp = Math.Clamp((power - shockWave.DampStartPower) / (shockWave.DampFullPower - shockWave.DampStartPower), 0, 1);
            var strength = MathHelper.Lerp(1, shockWave.MinStrength, damp);
            AddWave(args, shockWave, visuals.Epicenter.Position, ringRadius, strength);
        }

        return _count > 0;
    }

    private void AddWave(in OverlayDrawArgs args, RMCExplosionShockWaveVisuals shockWave, Vector2 worldPos, float radius, float strength)
    {
        var viewportSize = args.Viewport.Size;
        var local = args.Viewport.WorldToLocal(worldPos);
        var edge = args.Viewport.WorldToLocal(worldPos + new Vector2(radius, 0));

        // normalized coords, 0 - 1 plane. This is pure hell, we subtract 1 because fragment calculates from the bottom and local goes from the top of the viewport
        _positions[_count] = new Vector2(local.X / viewportSize.X, 1 - local.Y / viewportSize.Y);
        _radii[_count] = (edge - local).Length() / viewportSize.X;
        _falloffPower[_count] = shockWave.FalloffPower;
        _sharpness[_count] = shockWave.Sharpness;
        _width[_count] = shockWave.Width;
        _strength[_count] = strength;
        _count++;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;

        _shader.SetParameter("count", _count);
        _shader.SetParameter("position", _positions);
        _shader.SetParameter("radius", _radii);
        _shader.SetParameter("falloffPower", _falloffPower);
        _shader.SetParameter("sharpness", _sharpness);
        _shader.SetParameter("width", _width);
        _shader.SetParameter("strength", _strength);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }
}
