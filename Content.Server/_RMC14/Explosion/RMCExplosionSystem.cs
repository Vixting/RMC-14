using System.Linq;
using System.Numerics;
using Content.Server.Decals;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Voicelines;
using Content.Shared.Coordinates;
using Content.Shared.Decals;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Humanoid;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Sticky;
using Content.Shared.Tag;
using Robust.Server.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Explosion;

public sealed class RMCExplosionSystem : SharedRMCExplosionSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly HumanoidVoicelinesSystem _humanoidVoicelines = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    private readonly PriorityQueue<PendingScorch, TimeSpan> _pendingScorches = new();

    private readonly Dictionary<string, ProtoId<DecalPrototype>[]> _scorchDecalsByTag = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<ActiveTimerTriggerEvent>(OnActiveTimerTrigger);

        SubscribeLocalEvent<CMVocalizeTriggerComponent, ActiveTimerTriggerEvent>(OnVocalizeTriggered);

        SubscribeLocalEvent<RMCExplosiveDeleteComponent, EntityStuckEvent>(OnExplosiveDeleteWallsStuck);

        SubscribeLocalEvent<ExplosionVisualsComponent, RMCExplosionSpawnedEvent>(OnExplosionSpawned);
        SubscribeLocalEvent<ExplosionVisualsComponent, RMCExplosionFinishedEvent>(OnExplosionFinished);

        SubscribeLocalEvent<RandomTimerTriggerComponent, ExaminedEvent>(OnRandomTimerTriggerExamined);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _pendingScorches.Clear();
    }

    private void OnActiveTimerTrigger(ref ActiveTimerTriggerEvent ev)
    {
        var rmcEv = new RMCActiveTimerTriggerEvent();
        RaiseLocalEvent(ev.Triggered, ref rmcEv);
    }

    private void OnTrigger(TriggerEvent ev)
    {
        var rmcEv = new RMCTriggerEvent(ev.User, ev.Handled);
        RaiseLocalEvent(ev.Triggered, ref rmcEv);
        ev.Handled = rmcEv.Handled;
    }

    private void OnVocalizeTriggered(Entity<CMVocalizeTriggerComponent> ent, ref ActiveTimerTriggerEvent args)
    {
        SpawnAttachedTo(ent.Comp.Effect, ent.Owner.ToCoordinates());

        if (args.User is not { } user)
            return;

        var popup = Loc.GetString(ent.Comp.UserPopup, ("used", ent.Owner));
        _popup.PopupEntity(popup, user, user, PopupType.LargeCaution);

        popup = Loc.GetString(ent.Comp.OthersPopup, ("user", user), ("used", ent.Owner));
        _popup.PopupEntity(popup, user, Filter.PvsExcept(user), true, ent.Comp.PopupType);

        var gender = CompOrNull<HumanoidAppearanceComponent>(user)?.Sex ?? Sex.Unsexed;
        if (!ent.Comp.Sounds.TryGetValue(gender, out var sound))
            return;

        var filter = Filter.Pvs(user).RemoveWhere(s => !_humanoidVoicelines.ShouldPlayVoiceline(user, s));
        if (filter.Count == 0)
            return;

        _audio.PlayEntity(sound, filter, user, true);
    }

    private void OnExplosiveDeleteWallsStuck(Entity<RMCExplosiveDeleteComponent> ent, ref EntityStuckEvent args)
    {
        _trigger.HandleTimerTrigger(ent, args.User, ent.Comp.Delay, ent.Comp.BeepInterval, null, ent.Comp.BeepSound);
    }

    private void OnExplosionSpawned(Entity<ExplosionVisualsComponent> ent, ref RMCExplosionSpawnedEvent args)
    {
        ent.Comp.RMCStartTime = _timing.CurTime;
        ent.Comp.RMCParticlesOverride = args.ParticlesOverride;
        Dirty(ent);

        if (!_prototypeManager.TryIndex(ent.Comp.ExplosionType, out ExplosionPrototype? type) ||
            type.PowerPerIntensity <= 0)
        {
            return;
        }

        var scorch = type.Scorch;
        var noise = new FastNoiseLite(_random.Next());
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(scorch.NoiseFrequency);

        var time = _timing.CurTime;
        foreach (var (gridUid, iterations) in ent.Comp.Tiles)
        {
            foreach (var (iteration, tiles) in iterations)
            {
                if (iteration >= ent.Comp.Intensity.Count)
                    continue;

                var power = ent.Comp.Intensity[iteration] * type.PowerPerIntensity;
                var at = time + scorch.RingDelay * iteration;
                var core = iteration <= scorch.CoreRings;
                foreach (var tile in tiles)
                {
                    var density = Math.Clamp(noise.GetNoise(tile.X, tile.Y) * 0.5f + 0.5f, 0f, 1f);
                    _pendingScorches.Enqueue(new PendingScorch(scorch, gridUid, tile, power, density, core), at);
                }
            }
        }
    }

    private void OnExplosionFinished(Entity<ExplosionVisualsComponent> ent, ref RMCExplosionFinishedEvent args)
    {
        if (!_prototypeManager.TryIndex(ent.Comp.ExplosionType, out ExplosionPrototype? type))
            return;

        var visualLifetime = RMCExplosionVisuals.VisualLifetime(type, ent.Comp.Intensity);
        var remaining = visualLifetime - (_timing.CurTime - ent.Comp.RMCStartTime).TotalSeconds;
        args.Lifetime = Math.Max(args.Lifetime, (float) remaining);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        while (_pendingScorches.TryPeek(out _, out var at) && at <= time)
        {
            var scorch = _pendingScorches.Dequeue();
            var settings = scorch.Settings;
            var heavyDecals = GetDecals(settings.HeavyDecalTag);
            var lightDecals = GetDecals(settings.LightDecalTag);
            if (heavyDecals.Length == 0 ||
                lightDecals.Length == 0 ||
                !TryComp(scorch.Grid, out MapGridComponent? grid) ||
                RollScorch(settings, scorch.Power) is not { } heavy)
            {
                continue;
            }

            if (!scorch.Core)
            {
                var coverage = Math.Clamp(scorch.Power / settings.FullCoveragePower, settings.MinCoverage, settings.MaxCoverage);
                if (scorch.Density < 1 - coverage)
                    continue;

                if (heavy && scorch.Density < 1 - coverage * 0.5f)
                    heavy = false;
            }

            ScorchTile(settings, (scorch.Grid, grid), scorch.Tile, heavy, heavyDecals, lightDecals);
        }
    }

    private bool? RollScorch(RMCExplosionScorch settings, float power)
    {
        if (power >= settings.HeavyPower)
            return true;

        if (power >= settings.LightPower)
            return _random.Prob((power - settings.LightPower) / (settings.HeavyPower - settings.LightPower));

        return _random.Prob(Math.Max(0, power) / settings.LightPower) ? false : null;
    }

    private void ScorchTile(RMCExplosionScorch settings, Entity<MapGridComponent> grid, Vector2i tile, bool heavy, ProtoId<DecalPrototype>[] heavyDecals, ProtoId<DecalPrototype>[] lightDecals)
    {
        if (!_map.TryGetTileRef(grid, grid.Comp, tile, out var tileRef) ||
            tileRef.Tile.IsEmpty ||
            _tileDefinitions[tileRef.Tile.TypeId] is not ContentTileDefinition { Indestructible: false })
        {
            return;
        }

        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (_tag.HasTag(uid, WallTag) && !HasComp<DoorComponent>(uid))
                return;
        }

        var coordinates = _map.GridTileToLocal(grid, grid.Comp, tile).Offset(new Vector2(-0.5f, -0.5f));
        var bounds = Box2.CenteredAround(coordinates.Position, new Vector2(settings.Jitter * 2 + 0.1f));
        var existing = _decals.GetDecalsIntersecting(grid, bounds);
        foreach (var (_, decal) in existing)
        {
            if (heavyDecals.Contains(decal.Id))
                return;
        }

        if (!heavy && existing.Any(d => lightDecals.Contains(d.Decal.Id)))
            return;

        if (heavy)
        {
            foreach (var (index, decal) in existing)
            {
                if (lightDecals.Contains(decal.Id))
                    _decals.RemoveDecal(grid, index);
            }
        }

        var decals = heavy ? heavyDecals : lightDecals;
        var decalId = decals[_random.Next(decals.Length)];
        var jitter = new Vector2(
            _random.NextFloat(-settings.Jitter, settings.Jitter),
            _random.NextFloat(-settings.Jitter, settings.Jitter));
        var brightness = _random.NextFloat(settings.MinBrightness, 1f);
        var color = new Color(brightness, brightness, brightness, _random.NextFloat(settings.MinAlpha, 1f));
        _decals.TryAddDecal(decalId, coordinates.Offset(jitter), out _, color, _random.NextAngle(), cleanable: true);
    }

    private void OnRandomTimerTriggerExamined(Entity<RandomTimerTriggerComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RandomTimerTriggerComponent)))
        {
            args.PushMarkup($"[color=cyan]This will have a random timer between {ent.Comp.Min} and {ent.Comp.Max} seconds on use![/color]");
        }
    }

    public override void QueueExplosion(
        MapCoordinates epicenter,
        string typeId,
        float totalIntensity,
        float slope,
        float maxTileIntensity,
        EntityUid? cause,
        float tileBreakScale = 1f,
        int maxTileBreak = int.MaxValue,
        bool canCreateVacuum = true,
        bool addLog = true)
    {
        _explosion.QueueExplosion(
            epicenter,
            typeId,
            totalIntensity,
            slope,
            maxTileIntensity,
            cause,
            tileBreakScale,
            maxTileBreak,
            canCreateVacuum,
            addLog
        );
    }

    public override void TriggerExplosive(EntityUid uid,
        bool delete = true,
        float? totalIntensity = null,
        float? radius = null,
        EntityUid? user = null)
    {
        _explosion.TriggerExplosive(uid, null, delete, totalIntensity, radius, user);
    }

    private ProtoId<DecalPrototype>[] GetDecals(string decalTag)
    {
        if (_scorchDecalsByTag.TryGetValue(decalTag, out var cached))
            return cached;

        CacheDecals(decalTag);
        return _scorchDecalsByTag.TryGetValue(decalTag, out var decals) ? decals : Array.Empty<ProtoId<DecalPrototype>>();
    }

    private void CacheDecals(string decalTag)
    {
        var decals = _prototypeManager.EnumeratePrototypes<DecalPrototype>()
            .Where(x => x.Tags.Contains(decalTag))
            .Select(x => new ProtoId<DecalPrototype>(x.ID))
            .ToArray();

        _scorchDecalsByTag[decalTag] = decals;
        if (decals.Length == 0)
            Log.Error($"Failed to get any scorch decals. Check that at least one decal has tag {decalTag}.");
    }

    private readonly record struct PendingScorch(RMCExplosionScorch Settings, EntityUid Grid, Vector2i Tile, float Power, float Density, bool Core);
}
