using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameStates;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing; // RMC14
using Robust.Shared.Utility; // RMC14

namespace Content.Client.Explosion;

/// <summary>
///     This system is responsible for showing the client-side explosion effects (light source & fire-overlay). The
///     fire overlay code is just a bastardized version of the atmos plasma fire overlay and uses the same texture.
/// </summary>
public sealed class ExplosionOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!; // RMC14

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExplosionVisualsComponent, ComponentInit>(OnExplosionInit);
        SubscribeLocalEvent<ExplosionVisualsComponent, ComponentRemove>(OnCompRemove);
        SubscribeLocalEvent<ExplosionVisualsComponent, ComponentHandleState>(OnExplosionHandleState);
        _overlayMan.AddOverlay(new ExplosionOverlay(_appearance));
    }

    private void OnExplosionHandleState(EntityUid uid, ExplosionVisualsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ExplosionVisualsState state)
            return;

        component.Epicenter = state.Epicenter;
        component.SpaceTiles = state.SpaceTiles;
        component.Tiles.Clear();

        foreach (var (nent, data) in state.Tiles)
        {
            component.Tiles[GetEntity(nent)] = data;
        }

        component.Intensity = state.Intensity;
        component.ExplosionType = state.ExplosionType;
        component.SpaceMatrix = state.SpaceMatrix;
        component.SpaceTileSize = state.SpaceTileSize;
        component.RMCStartTime = state.RMCStartTime; // RMC14
        component.RMCTinyParticles = state.RMCTinyParticles; // RMC14

        // RMC14
        if (TryComp(uid, out ExplosionVisualsTexturesComponent? textures) && state.RMCStartTime > TimeSpan.Zero)
            textures.RMCStartTime = state.RMCStartTime;
        // RMC14
    }

    private void OnCompRemove(EntityUid uid, ExplosionVisualsComponent component, ComponentRemove args)
    {
        if (TryComp(uid, out ExplosionVisualsTexturesComponent? textures) && !Deleted(textures.LightEntity))
            QueueDel(textures.LightEntity);
    }

    private void OnExplosionInit(EntityUid uid, ExplosionVisualsComponent component, ComponentInit args)
    {
        EnsureComp<ExplosionVisualsTexturesComponent>(uid);

        if (!_protoMan.TryIndex(component.ExplosionType, out ExplosionPrototype? type) ||
            !TryComp(uid, out ExplosionVisualsTexturesComponent? textures))
        {
            return;
        }

        // Map may have been deleted.
        if (_mapSystem.MapExists(component.Epicenter.MapId))
        {
            // spawn in a client-side light source at the epicenter
            var lightEntity = Spawn("ExplosionLight", component.Epicenter);
            var light = _lights.EnsureLight(lightEntity);

            _lights.SetRadius(lightEntity, component.Intensity.Count, light);
            _lights.SetEnergy(lightEntity, component.Intensity.Count, light);
            _lights.SetColor(lightEntity, type.LightColor, light);
            textures.LightEntity = lightEntity;
        }


        textures.FireColor = type.FireColor;
        textures.IntensityPerState = type.IntensityPerState;
        // RMC14
        textures.RMCPowerPerIntensity = type.PowerPerIntensity;
        textures.RMCStartTime = component.RMCStartTime > TimeSpan.Zero ? component.RMCStartTime : _timing.CurTime;
        textures.RMCHeatColor = type.LightColor;
        textures.RMCSeed = uid.Id;
        if (type.PowerPerIntensity > 0)
        {
            AddAllFrames(textures.RMCSmokeFrames, type.TexturePath);
            if (type.CoreTexturePath is { } corePath)
                AddAllFrames(textures.RMCCoreFrames, corePath);

            if (type.SparkTexturePath is { } sparkPath)
            {
                var sparkRsi = _resCache.GetResource<RSIResource>(sparkPath).RSI;
                foreach (var sparkState in type.SparkStates)
                {
                    if (sparkRsi.TryGetState(sparkState, out var state))
                        textures.RMCSparkFrames.Add(state.GetFrames(RsiDirection.South));
                }
            }
        }
        // RMC14

        var fireRsi = _resCache.GetResource<RSIResource>(type.TexturePath).RSI;
        foreach (var state in fireRsi)
        {
            textures.FireFrames.Add(state.GetFrames(RsiDirection.South));
            if (textures.FireFrames.Count == type.FireStates)
                break;
        }
    }

    // RMC14
    private void AddAllFrames(List<Texture> frames, ResPath path)
    {
        var rsi = _resCache.GetResource<RSIResource>(path).RSI;
        foreach (var state in rsi)
        {
            foreach (var direction in state.Icons)
            {
                frames.AddRange(direction);
            }
        }
    }
    // RMC14

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<ExplosionOverlay>();
    }
}
