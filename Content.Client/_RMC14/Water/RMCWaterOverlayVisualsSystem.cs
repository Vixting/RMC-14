using System.Numerics;
using Content.Shared._RMC14.Water;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Standing;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Water;

public sealed class RMCWaterOverlayVisualsSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCWaterOverlaySystem _waterOverlay = default!;

    private const string OverlayLayerKey = "rmc-water-overlay";
    private const string SinkAnimationKey = "rmc-water-sink";
    private const float SinkAnimationDuration = 0.2f;
    private static readonly ProtoId<ShaderPrototype> SubmersionShader = "RMCWaterSubmersion";

    private static readonly Color NormalTint = Color.White.WithAlpha(0.7f);
    private static readonly Color ToxicTint = new(0.6f, 0.85f, 0.35f, 0.7f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCWaterOverlayComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCWaterOverlayComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<RMCWaterOverlayComponent, WaterOverlayChangedEvent>(OnChanged);

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

        ApplyLayer(uid, sprite, bucket, depth, toxic);
        ApplySubmersionShader(uid, sprite, depth);
        AnimateSink(uid, sprite, depth);
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
        var rsiPath = $"/Textures/_RMC14/Effects/WaterOverlay/_{bucket}.rsi";
        var layerData = new PrototypeLayerData
        {
            RsiPath = rsiPath,
            State = GetStateName(uid, depth),
            Color = toxic ? ToxicTint : NormalTint,
            Visible = true,
        };

        if (_sprite.LayerMapTryGet((uid, sprite), OverlayLayerKey, out var index, false))
        {
            _sprite.LayerSetData((uid, sprite), index, layerData);
        }
        else
        {
            index = _sprite.AddLayer((uid, sprite), layerData, null);
            _sprite.LayerMapSet((uid, sprite), OverlayLayerKey, index);
        }
    }

    private void ApplySubmersionShader(EntityUid uid, SpriteComponent sprite, WaterDepth depth)
    {
        if (!TryComp(uid, out RMCWaterOverlayVisualsComponent? visuals))
            return;

        visuals.SubmersionShader ??= _proto.Index(SubmersionShader).InstanceUnique();

        if (sprite.PostShader is not null && sprite.PostShader != visuals.SubmersionShader)
            return;

        var cutoff = IsResting(uid) ? GetRestingSubmersionCutoff(depth) : GetSubmersionCutoff(depth);
        visuals.SubmersionShader.SetParameter("cutoff", cutoff);
        sprite.PostShader = visuals.SubmersionShader;
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

    private static float GetRestingSubmersionCutoff(WaterDepth depth)
    {
        return depth switch
        {
            WaterDepth.CoastShallow => 0.2f,
            WaterDepth.CoastDeep => 0.2f,
            WaterDepth.Shallow => 0.35f,
            WaterDepth.Intermediate => 0.5f,
            WaterDepth.Deep => 0.60f,
            _ => 0f,
        };
    }

    private void RemoveLayer(EntityUid uid, SpriteComponent sprite)
    {
        if (_sprite.LayerMapTryGet((uid, sprite), OverlayLayerKey, out var index, false))
        {
            _sprite.RemoveLayer((uid, sprite), index);
            _sprite.LayerMapRemove((uid, sprite), OverlayLayerKey);
        }

        if (!TryComp(uid, out RMCWaterOverlayVisualsComponent? visuals))
            return;

        if (visuals.SubmersionShader is not null && sprite.PostShader == visuals.SubmersionShader)
            sprite.PostShader = null;

        if (TryComp(uid, out AnimationPlayerComponent? player))
            _animPlayer.Stop((uid, player), SinkAnimationKey);

        _sprite.SetOffset((uid, sprite), visuals.OriginalOffset);
    }

    private void AnimateSink(EntityUid uid, SpriteComponent sprite, WaterDepth depth)
    {
        if (!TryComp(uid, out RMCWaterOverlayVisualsComponent? visuals))
            return;

        var target = visuals.OriginalOffset + new Vector2(0f, WaterDepthOffsets.GetSpriteOffset(depth));
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

    private string GetStateName(EntityUid uid, WaterDepth depth)
    {
        if (IsResting(uid))
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

        var dir = _transform.GetWorldRotation(uid).GetCardinalDir();
        var facingEast = dir is Direction.East or Direction.NorthEast or Direction.SouthEast;
        return facingEast ? "coast_resting_e" : "coast_resting_w";
    }
}
