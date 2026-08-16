using System.Numerics;
using Vector4 = Robust.Shared.Maths.Vector4;
using Content.Client.DisplacementMap;
using Content.Shared._RMC14.Water;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.DisplacementMap;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Animations;
using Robust.Shared.ContentPack;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Water;

public sealed class RMCWaterOverlayVisualsSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCWaterOverlaySystem _waterOverlay = default!;

    private const string OverlayLayerKey = "rmc-water-overlay";
    private const string CullMaskLayerKey = OverlayLayerKey + "-displacement";
    private const string SinkAnimationKey = "rmc-water-sink";
    private const float SinkAnimationDuration = 0.2f;

    private static readonly ProtoId<ShaderPrototype> SubmersionShader = "RMCWaterSubmersion";
    private static readonly ProtoId<ShaderPrototype> SubmersionMaskShader = "RMCWaterSubmersionMask";
    private static readonly ProtoId<ShaderPrototype> OverlayCullShader = "RMCWaterOverlayCull";

    private static readonly Color NormalTint = Color.White;
    private static readonly Color ToxicTint = new(0.6f, 0.85f, 0.35f, 1f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCWaterOverlayComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCWaterOverlayComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<RMCWaterOverlayComponent, WaterOverlayChangedEvent>(OnChanged);
        SubscribeLocalEvent<RMCWaterOverlayComponent, SpriteMoveEvent>(OnSpriteMove);

        SubscribeLocalEvent<RMCWaterOverlayComponent, DownedEvent>(OnPoseChanged);
        SubscribeLocalEvent<RMCWaterOverlayComponent, StoodEvent>(OnPoseChanged);
        SubscribeLocalEvent<XenoRestingComponent, ComponentStartup>(OnXenoRestingChanged);
        SubscribeLocalEvent<XenoRestingComponent, ComponentShutdown>(OnXenoRestingChanged);
    }

    private void OnStartup(Entity<RMCWaterOverlayComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var visuals = EnsureComp<RMCWaterOverlayVisualsComponent>(ent);
        visuals.OriginalOffset = sprite.Offset;

        Refresh(ent.Owner, sprite);
    }

    private void OnState(Entity<RMCWaterOverlayComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        Refresh(ent.Owner, sprite);
    }

    private void OnChanged(Entity<RMCWaterOverlayComponent> ent, ref WaterOverlayChangedEvent args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        Refresh(ent.Owner, sprite);
    }

    private void OnPoseChanged<T>(Entity<RMCWaterOverlayComponent> ent, ref T args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        Refresh(ent.Owner, sprite);
    }

    private void OnXenoRestingChanged(Entity<XenoRestingComponent> ent, ref ComponentStartup args)
    {
        RefreshIfPresent(ent.Owner);
    }

    private void OnXenoRestingChanged(Entity<XenoRestingComponent> ent, ref ComponentShutdown args)
    {
        RefreshIfPresent(ent.Owner);
    }

    private void OnSpriteMove(Entity<RMCWaterOverlayComponent> ent, ref SpriteMoveEvent args)
    {
        if (!ent.Comp.InWater || !TryComp(ent, out SpriteComponent? sprite))
            return;

        SetWading(ent.Owner, sprite, IsResting(ent.Owner) || args.IsMoving);
    }

    private void SetWading(EntityUid uid, SpriteComponent sprite, bool wading)
    {
        if (!_sprite.TryGetLayer((uid, sprite), OverlayLayerKey, out var layer, false))
            return;

        if (layer.Visible == wading && layer.AutoAnimated == wading)
            return;

        // AutoAnimated=false pauses the layer's animation clock entirely while hidden (it doesn't keep
        // advancing in the background), so leaving it alone here means the ripple resumes from wherever
        // it actually left off instead of always restarting from frame 0's own pose held for its own
        // delay - which looked like the overlay freezing for a moment every time wading started.
        _sprite.LayerSetVisible((uid, sprite), OverlayLayerKey, wading);
        _sprite.LayerSetAutoAnimated((uid, sprite), OverlayLayerKey, wading);
    }

    private void RefreshIfPresent(EntityUid uid)
    {
        if (!HasComp<RMCWaterOverlayComponent>(uid) || !TryComp(uid, out SpriteComponent? sprite))
            return;

        Refresh(uid, sprite);
    }

    private void Refresh(EntityUid uid, SpriteComponent sprite)
    {
        if (!_waterOverlay.IsInWater(uid))
        {
            RemoveLayer(uid, sprite);
            return;
        }

        var bucket = GetBucket(uid, sprite);
        var depth = _waterOverlay.GetEffectiveDepth(uid);
        var toxic = _waterOverlay.IsOverlayToxic(uid);

        Log.Info($"[WaterCull] Refresh {ToPrettyString(uid)} t={_timing.RealTime.TotalMilliseconds:F0}ms bucket={bucket} depth={depth} toxic={toxic} resting={IsResting(uid)}");

        ApplyLayer(uid, sprite, bucket, depth, toxic);
        ApplySubmersionShader(uid, sprite, bucket, depth);
        AnimateSink(uid, sprite, depth);

        var moving = TryComp(uid, out InputMoverComponent? mover) && mover.HasDirectionalMovement;
        SetWading(uid, sprite, IsResting(uid) || moving);
    }

    private int GetBucket(EntityUid uid, SpriteComponent sprite)
    {
        if (TryComp(uid, out WaterOverlaySizeOverrideComponent? sizeOverride))
            return sizeOverride.Size;

        var rsi = _sprite.LayerGetEffectiveRsi((uid, sprite), 0);
        var size = rsi?.Size.X ?? 32;

        if (size <= 32)
            return 32;
        if (size <= 48)
            return 48;
        if (size <= 64)
            return 64;

        return 88;
    }

    private void ApplyLayer(EntityUid uid, SpriteComponent sprite, int bucket, WaterDepth depth, bool toxic)
    {
        var state = GetStateName(uid, bucket, depth);
        var color = (toxic ? ToxicTint : NormalTint).WithAlpha(GetDepthAlpha(depth));
        var rsiPath = $"/Textures/_RMC14/Effects/WaterOverlay/_{bucket}.rsi";
        TryComp(uid, out RMCWaterOverlayVisualsComponent? visuals);

        if (!_sprite.LayerMapTryGet((uid, sprite), OverlayLayerKey, out var index, false))
        {
            var layerData = new PrototypeLayerData
            {
                RsiPath = rsiPath,
                State = state,
                Color = color,
                Visible = true,
            };

            index = _sprite.AddLayer((uid, sprite), layerData, null);
            _sprite.LayerMapSet((uid, sprite), OverlayLayerKey, index);

            if (visuals is not null)
                visuals.OverlayBucket = bucket;
        }
        else if (visuals is null || visuals.OverlayBucket != bucket)
        {
            _sprite.LayerSetRsi((uid, sprite), index, new ResPath(rsiPath), state);

            if (visuals is not null)
                visuals.OverlayBucket = bucket;
        }
        else
        {
            _sprite.LayerSetRsiState((uid, sprite), index, state);
        }

        _sprite.LayerSetColor((uid, sprite), index, color);
        _sprite.LayerSetVisible((uid, sprite), index, true);

        ApplyCullMask(uid, sprite, index, bucket, depth);
    }

    private void ApplyCullMask(EntityUid uid, SpriteComponent sprite, int overlayIndex, int bucket, WaterDepth depth)
    {
        var rsiPath = $"/Textures/_RMC14/Effects/WaterOverlay/_{bucket}.rsi";
        var state = bucket == 32 ? GetCullingStateName(uid, depth) : "culling_mask";
        var keepWhereOpaque = state != "culling_mask";

        if (TryComp(uid, out RMCWaterOverlayVisualsComponent? visuals))
        {
            visuals.CullShader ??= _proto.Index(OverlayCullShader).InstanceUnique();
            visuals.CullShader.SetParameter("keepWhereOpaque", keepWhereOpaque);
            sprite.LayerSetShader(overlayIndex, visuals.CullShader);
        }

        if (_sprite.LayerMapTryGet((uid, sprite), CullMaskLayerKey, out var cullIndex, false))
        {
            Log.Info($"[WaterCull] {ToPrettyString(uid)} updating mask layer index={cullIndex} state={state} keepWhereOpaque={keepWhereOpaque}");
            _sprite.LayerSetRsi((uid, sprite), cullIndex, new ResPath(rsiPath), state);
            return;
        }

        var cullData = new DisplacementData
        {
            ShaderOverride = null,
            SizeMaps = new Dictionary<int, PrototypeLayerData>
            {
                [32] = new PrototypeLayerData
                {
                    RsiPath = rsiPath,
                    State = state,
                    Visible = true,
                    CopyToShaderParameters = new PrototypeCopyToShaderParameters
                    {
                        LayerKey = OverlayLayerKey,
                        ParameterTexture = "cullMask",
                        ParameterUV = "cullMaskUV",
                    },
                },
            },
        };

        var added = _displacement.TryAddDisplacement(cullData, (uid, sprite), overlayIndex, OverlayLayerKey, out var displacementKey);
        Log.Info($"[WaterCull] {ToPrettyString(uid)} created mask layer overlayIndex={overlayIndex} rsi={rsiPath} state={state} keepWhereOpaque={keepWhereOpaque} added={added} key={displacementKey}");

        if (_sprite.TryGetLayer((uid, sprite), OverlayLayerKey, out var overlayLayer, false))
        {
            var sameShader = ReferenceEquals(overlayLayer.Shader, visuals?.CullShader);
            var mutable = overlayLayer.Shader?.Mutable;
            Log.Info($"[WaterCull] {ToPrettyString(uid)} overlay layer after add: visible={overlayLayer.Visible} shader={(overlayLayer.Shader is null ? "null" : overlayLayer.Shader.GetType().Name)} sameInstanceAsCullShader={sameShader} mutable={mutable}");
        }

        if (_sprite.TryGetLayer((uid, sprite), CullMaskLayerKey, out var maskLayer, false))
            Log.Info($"[WaterCull] {ToPrettyString(uid)} mask layer after add: visible={maskLayer.Visible} blank={maskLayer.Blank} state={maskLayer.State}");

        DumpLayerStack(uid, sprite);
    }

    private void DumpLayerStack(EntityUid uid, SpriteComponent sprite)
    {
        var i = 0;
        foreach (var layer in sprite.AllLayers)
        {
            Log.Info($"[WaterCull] {ToPrettyString(uid)} layer[{i}] visible={layer.Visible} state={layer.RsiState} color={layer.Color}");
            i++;
        }
    }

    private void ApplySubmersionShader(EntityUid uid, SpriteComponent sprite, int bucket, WaterDepth depth)
    {
        if (!TryComp(uid, out RMCWaterOverlayVisualsComponent? visuals))
            return;

        var mask = bucket == 32 ? GetSubmersionMask(uid, depth) : null;
        var target = mask is null ? SelectSubmersionShader(visuals) : SelectSubmersionMaskShader(visuals);

        var ownShader = sprite.PostShader is null || sprite.PostShader == visuals.SubmersionShader || sprite.PostShader == visuals.SubmersionMaskShader;
        if (!ownShader)
            return;

        if (mask is { } found)
        {
            Log.Info($"[WaterCull] {ToPrettyString(uid)} submersion mask applied: sprite.Rotation={sprite.Rotation} (theta={sprite.Rotation.Theta}) worldRotation={_transform.GetWorldRotation(uid)}");
            target.SetParameter("mask", found.Texture);
            target.SetParameter("maskUV", found.Uv);
            target.SetParameter("spriteRotation", (float) sprite.Rotation.Theta);
        }
        else
        {
            target.SetParameter("cutoff", GetSubmersionCutoff(depth));
        }

        target.SetParameter("alphaModifier", GetSubmersionAlphaModifier(depth));
        sprite.PostShader = target;
    }

    private ShaderInstance SelectSubmersionShader(RMCWaterOverlayVisualsComponent visuals)
    {
        return visuals.SubmersionShader ??= _proto.Index(SubmersionShader).InstanceUnique();
    }

    private ShaderInstance SelectSubmersionMaskShader(RMCWaterOverlayVisualsComponent visuals)
    {
        return visuals.SubmersionMaskShader ??= _proto.Index(SubmersionMaskShader).InstanceUnique();
    }

    private (Texture Texture, Vector4 Uv)? GetSubmersionMask(EntityUid uid, WaterDepth depth)
    {
        if (!IsResting(uid))
            return null;

        var state = GetCullingStateName(uid, depth);
        if (state == "culling_mask")
            return null;

        if (!_resourceCache.TryGetResource<RSIResource>(new ResPath("/Textures/_RMC14/Effects/WaterOverlay/_32.rsi"), out var rsiRes))
            return null;

        if (!rsiRes.RSI.TryGetState(state, out var rsiState))
            return null;

        var angle = _transform.GetWorldRotation(uid);
        var rsiDirection = SpriteComponent.Layer.GetDirection(rsiState.RsiDirections, angle);
        var texture = rsiState.GetFrame(rsiDirection, 0);

        if (texture is not AtlasTexture atlas)
            return (texture, new Vector4(0f, 0f, 1f, 1f));

        var source = atlas.SourceTexture;
        var region = atlas.SubRegion;
        var uv = new Vector4(
            region.Left / source.Width,
            (source.Height - region.Bottom) / source.Height,
            region.Right / source.Width,
            (source.Height - region.Top) / source.Height);

        return (source, uv);
    }

    private static float GetSubmersionCutoff(WaterDepth depth)
    {
        return depth switch
        {
            WaterDepth.CoastShallow => 0.23f,
            WaterDepth.CoastDeep => 0.26f,
            WaterDepth.Shallow => 0.4f,
            WaterDepth.Intermediate => 0.48f,
            WaterDepth.Deep => 0.63f,
            _ => 0f,
        };
    }

    private static float GetDepthAlpha(WaterDepth depth)
    {
        return depth switch
        {
            WaterDepth.CoastShallow => 0.45f,
            WaterDepth.CoastDeep => 0.6f,
            WaterDepth.Shallow => 0.55f,
            WaterDepth.Intermediate => 0.72f,
            WaterDepth.Deep => 0.85f,
            _ => 0.6f,
        };
    }

    private static float GetSubmersionAlphaModifier(WaterDepth depth)
    {
        return depth switch
        {
            WaterDepth.CoastShallow => 0.4f,
            WaterDepth.CoastDeep => 0.3f,
            WaterDepth.Shallow => 0.2f,
            WaterDepth.Intermediate => 0.12f,
            WaterDepth.Deep => 0.05f,
            _ => 0.15f,
        };
    }

    private void RemoveLayer(EntityUid uid, SpriteComponent sprite)
    {
        if (_sprite.LayerMapTryGet((uid, sprite), CullMaskLayerKey, out var cullIndex, false))
        {
            _sprite.RemoveLayer((uid, sprite), cullIndex);
            _sprite.LayerMapRemove((uid, sprite), CullMaskLayerKey);
        }

        if (_sprite.LayerMapTryGet((uid, sprite), OverlayLayerKey, out var index, false))
        {
            _sprite.RemoveLayer((uid, sprite), index);
            _sprite.LayerMapRemove((uid, sprite), OverlayLayerKey);
        }

        if (!TryComp(uid, out RMCWaterOverlayVisualsComponent? visuals))
            return;

        if (sprite.PostShader == visuals.SubmersionShader || sprite.PostShader == visuals.SubmersionMaskShader)
            sprite.PostShader = null;

        if (TryComp(uid, out AnimationPlayerComponent? player))
            _animPlayer.Stop((uid, player), SinkAnimationKey);

        _sprite.SetOffset((uid, sprite), visuals.OriginalOffset);
    }

    private void AnimateSink(EntityUid uid, SpriteComponent sprite, WaterDepth depth)
    {
        if (!TryComp(uid, out RMCWaterOverlayVisualsComponent? visuals))
            return;

        var offset = IsResting(uid) ? 0f : WaterDepthOffsets.GetSpriteOffset(depth);
        var target = visuals.OriginalOffset + new Vector2(0f, offset);
        var player = EnsureComp<AnimationPlayerComponent>(uid);

        var alreadyThere = sprite.Offset == target;
        var running = _animPlayer.HasRunningAnimation(uid, player, SinkAnimationKey);
        if (alreadyThere && !running)
            return;

        if (running)
            _animPlayer.Stop((uid, player), SinkAnimationKey);

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(SinkAnimationDuration),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Offset, 0f),
                        new AnimationTrackProperty.KeyFrame(target, SinkAnimationDuration),
                    },
                },
            },
        };

        _animPlayer.Play((uid, player), animation, SinkAnimationKey);
    }

    private string GetStateName(EntityUid uid, int bucket, WaterDepth depth)
    {
        if (bucket == 32 && IsResting(uid))
            return GetRestingStateName(uid, depth);

        return depth switch
        {
            WaterDepth.CoastShallow => "coast_shallow",
            WaterDepth.CoastDeep => "coast_deep",
            WaterDepth.Shallow => "shallow",
            WaterDepth.Intermediate => "intermediate",
            WaterDepth.Deep => "deep",
            _ => "shallow",
        };
    }

    private bool IsResting(EntityUid uid)
    {
        return HasComp<XenoRestingComponent>(uid) || _standing.IsDown(uid);
    }

    private string GetRestingStateName(EntityUid uid, WaterDepth depth)
    {
        if (depth == WaterDepth.Deep)
            return "bubbles";

        if (depth == WaterDepth.Intermediate)
            return "floating_resting";

        return IsFacingEast(uid) ? "coast_resting_e" : "coast_resting_w";
    }

    private bool IsFacingEast(EntityUid uid)
    {
        var dir = _transform.GetWorldRotation(uid).GetCardinalDir();
        return dir is Direction.East or Direction.NorthEast or Direction.SouthEast;
    }

    private string GetCullingStateName(EntityUid uid, WaterDepth depth)
    {
        if (!IsResting(uid) || depth == WaterDepth.Deep)
            return "culling_mask";

        var facingEast = IsFacingEast(uid);

        if (depth == WaterDepth.Intermediate)
            return facingEast ? "culling_float_rest_e" : "culling_float_rest_w";

        return facingEast ? "culling_coast_rest_e" : "culling_coast_rest_w";
    }
}
