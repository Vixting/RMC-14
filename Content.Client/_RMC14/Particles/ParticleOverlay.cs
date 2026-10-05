using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Particles;

/// <summary>Draws all live particles for every active emitter each frame.</summary>
public sealed class ParticleOverlay : Overlay
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly ParticleSystem _system;
    private readonly Dictionary<string, ShaderInstance?> _shaderCache = new();
    private readonly List<ActiveEmitter> _sortBuffer = new();

    private static readonly Comparison<ActiveEmitter> RenderLayerComparison =
        (a, b) => (a.Overrides?.RenderLayer ?? a.Proto.RenderLayer)
            .CompareTo(b.Overrides?.RenderLayer ?? b.Proto.RenderLayer);

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public ParticleOverlay(ParticleSystem system)
    {
        IoCManager.InjectDependencies(this);
        _system = system;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var mapId = args.MapId;
        var eyeAngle = (float)_eye.CurrentEye.Rotation;
        var cosR = MathF.Cos(-eyeAngle);
        var sinR = MathF.Sin(-eyeAngle);

        _sortBuffer.Clear();
        foreach (var emitter in _system.GetEmitters())
        {
            if (emitter.MapCoords.MapId != mapId) continue;
            if (!args.WorldBounds.Contains(emitter.MapCoords.Position)) continue;
            if (emitter.Frames.Length == 0) continue;
            _sortBuffer.Add(emitter);
        }

        if (_sortBuffer.Count == 0)
            return;

        _sortBuffer.Sort(RenderLayerComparison);

        string? activeShader = null;

        foreach (var emitter in _sortBuffer)
        {
            var proto = emitter.Proto;
            var ovr = emitter.Overrides;
            var tex = emitter.Frames[emitter.AnimFrame];
            var baseHalfSize = (ovr?.ParticleSize ?? proto.ParticleSize) * 0.5f;

            string? wantedShader = ovr?.Shader ?? (string.IsNullOrEmpty(proto.Shader) ? null : proto.Shader);
            if (wantedShader != activeShader)
            {
                if (wantedShader != null)
                {
                    if (!_shaderCache.TryGetValue(wantedShader, out var cached))
                    {
                        cached = _proto.TryIndex<ShaderPrototype>(wantedShader, out var shaderProto)
                            ? shaderProto.Instance()
                            : null;
                        _shaderCache[wantedShader] = cached;
                    }
                    handle.UseShader(cached);
                }
                else
                {
                    handle.UseShader(null);
                }
                activeShader = wantedShader;
            }

            var screenOrigin = emitter.MapCoords.Position;

            foreach (var particle in emitter.Particles)
            {
                if (!particle.Alive) continue;

                var t = particle.AgeRatio;

                Color color;
                if (proto.ColorOverLifetime.Count > 0)
                    color = ParticleSystem.SampleColorCurve(proto.ColorOverLifetime, t);
                else
                {
                    var startColor = ovr?.StartColor ?? proto.StartColor;
                    var endColor   = ovr?.EndColor   ?? proto.EndColor;
                    color = Color.InterpolateBetween(startColor, endColor, t);
                }

                var tintColor = ovr?.ColorOverride ?? emitter.ColorOverride;
                if (tintColor is { } tint)
                    color = new Color(color.R * tint.R, color.G * tint.G, color.B * tint.B, color.A * tint.A);

                if (proto.AlphaOverLifetime.Count > 0)
                {
                    var alpha = ParticleSystem.SampleCurve(proto.AlphaOverLifetime, t);
                    color = color.WithAlpha(color.A * alpha);
                }

                var halfSize = baseHalfSize * particle.SpawnIntensity * particle.SizeMultiplier;
                if (proto.SizeOverLifetime.Count > 0)
                    halfSize *= ParticleSystem.SampleCurve(proto.SizeOverLifetime, t);

                var local = particle.LocalOffset;
                var worldOffset = new Vector2(local.X * cosR - local.Y * sinR,
                                              local.X * sinR + local.Y * cosR);
                var origin = proto.WorldSpace ? particle.SpawnOrigin : screenOrigin;
                var worldPos = origin + worldOffset;

                var stretchFactor = ovr?.StretchFactor ?? proto.StretchFactor;
                if (stretchFactor > 0f)
                {
                    var velLenSq = particle.Velocity.LengthSquared();
                    if (velLenSq > 0.001f * 0.001f)
                    {
                        var velLen = MathF.Sqrt(velLenSq);
                        var stretchY = 1f + velLen * stretchFactor;
                        var invLen = 1f / velLen;
                        var ux = particle.Velocity.X * invLen;
                        var uy = particle.Velocity.Y * invLen;
                        var cV = cosR * uy - sinR * ux;
                        var sV = sinR * uy + cosR * ux;
                        handle.SetTransform(new Matrix3x2(cV, sV, -sV, cV, worldPos.X, worldPos.Y));
                        handle.DrawTextureRect(tex, new Box2(-halfSize, -halfSize * stretchY, halfSize, halfSize * stretchY), color);
                        continue;
                    }
                }

                if (proto.AlignToVelocity)
                {
                    var velLenSq = particle.Velocity.LengthSquared();
                    if (velLenSq > 0.001f * 0.001f)
                    {
                        var invLen = 1f / MathF.Sqrt(velLenSq);
                        var ux = particle.Velocity.X * invLen;
                        var uy = particle.Velocity.Y * invLen;
                        var cos = cosR * uy - sinR * ux;
                        var sin = sinR * uy + cosR * ux;
                        handle.SetTransform(new Matrix3x2(cos, sin, -sin, cos, worldPos.X, worldPos.Y));
                        handle.DrawTextureRect(tex, new Box2(-halfSize, -halfSize, halfSize, halfSize), color);
                        continue;
                    }
                }

                var totalRotation = -eyeAngle + particle.Rotation;
                var cosP = MathF.Cos(totalRotation);
                var sinP = MathF.Sin(totalRotation);
                handle.SetTransform(new Matrix3x2(cosP, sinP, -sinP, cosP, worldPos.X, worldPos.Y));
                handle.DrawTextureRect(tex, new Box2(-halfSize, -halfSize, halfSize, halfSize), color);
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(null);
    }
}
