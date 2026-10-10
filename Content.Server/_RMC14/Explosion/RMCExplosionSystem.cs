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

    private const string HeavyScorchTag = "RMCScorch";
    private const string LightScorchTag = "RMCScorchSmall";
    private const float ExplosionScorchHeavyPower = 200;
    private const float ExplosionScorchLightPower = 100;
    private const int ExplosionScorchCoreRings = 1;
    private const float ExplosionScorchNoiseFrequency = 0.18f;
    private const float ExplosionScorchFullCoveragePower = 600;
    private const float ExplosionScorchMinCoverage = 0.35f;
    private const float ExplosionScorchMaxCoverage = 0.85f;
    private const float ExplosionScorchJitter = 0.25f;
    private const float ExplosionScorchMinAlpha = 0.6f;
    private const float ExplosionScorchMinBrightness = 0.8f;
    private const double ExplosionScorchRingSeconds = 0.015;
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    private readonly List<PendingScorch> _pendingScorches = new();

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

        CacheDecals(HeavyScorchTag);
        CacheDecals(LightScorchTag);
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
        ent.Comp.RMCTinyParticles = args.TinyParticles;
        Dirty(ent);

        if (!_prototypeManager.TryIndex(ent.Comp.ExplosionType, out ExplosionPrototype? type) ||
            type.PowerPerIntensity <= 0)
        {
            return;
        }

        var noise = new FastNoiseLite(_random.Next());
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(ExplosionScorchNoiseFrequency);

        var time = _timing.CurTime;
        foreach (var (gridUid, iterations) in ent.Comp.Tiles)
        {
            foreach (var (iteration, tiles) in iterations)
            {
                if (iteration >= ent.Comp.Intensity.Count)
                    continue;

                var power = ent.Comp.Intensity[iteration] * type.PowerPerIntensity;
                var at = time + TimeSpan.FromSeconds(ExplosionScorchRingSeconds * iteration);
                var core = iteration <= ExplosionScorchCoreRings;
                foreach (var tile in tiles)
                {
                    var density = Math.Clamp(noise.GetNoise(tile.X, tile.Y) * 0.5f + 0.5f, 0f, 1f);
                    _pendingScorches.Add(new PendingScorch(at, gridUid, tile, power, density, core));
                }
            }
        }
    }

    private void OnExplosionFinished(Entity<ExplosionVisualsComponent> ent, ref RMCExplosionFinishedEvent args)
    {
        if (!_prototypeManager.TryIndex(ent.Comp.ExplosionType, out ExplosionPrototype? type))
            return;

        var visualLifetime = RMCExplosionVisuals.VisualLifetime(ent.Comp.Intensity, type.PowerPerIntensity);
        var remaining = visualLifetime - (_timing.CurTime - ent.Comp.RMCStartTime).TotalSeconds;
        args.Lifetime = Math.Max(args.Lifetime, (float) remaining);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pendingScorches.Count == 0)
            return;

        var heavyDecals = GetDecals(HeavyScorchTag);
        var lightDecals = GetDecals(LightScorchTag);
        var time = _timing.CurTime;
        for (var i = _pendingScorches.Count - 1; i >= 0; i--)
        {
            var scorch = _pendingScorches[i];
            if (scorch.At > time)
                continue;

            _pendingScorches.RemoveAt(i);
            if (heavyDecals.Length == 0 ||
                lightDecals.Length == 0 ||
                !TryComp(scorch.Grid, out MapGridComponent? grid) ||
                RollScorch(scorch.Power) is not { } heavy)
            {
                continue;
            }

            if (!scorch.Core)
            {
                var coverage = Math.Clamp(scorch.Power / ExplosionScorchFullCoveragePower, ExplosionScorchMinCoverage, ExplosionScorchMaxCoverage);
                if (scorch.Density < 1 - coverage)
                    continue;

                if (heavy && scorch.Density < 1 - coverage * 0.5f)
                    heavy = false;
            }

            ScorchTile((scorch.Grid, grid), scorch.Tile, heavy, heavyDecals, lightDecals);
        }
    }

    private bool? RollScorch(float power)
    {
        if (power >= ExplosionScorchHeavyPower)
            return true;

        if (power >= ExplosionScorchLightPower)
            return _random.Prob((power - ExplosionScorchLightPower) / (ExplosionScorchHeavyPower - ExplosionScorchLightPower));

        return _random.Prob(Math.Max(0, power) / ExplosionScorchLightPower) ? false : null;
    }

    private void ScorchTile(Entity<MapGridComponent> grid, Vector2i tile, bool heavy, ProtoId<DecalPrototype>[] heavyDecals, ProtoId<DecalPrototype>[] lightDecals)
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
        var bounds = Box2.CenteredAround(coordinates.Position, new Vector2(ExplosionScorchJitter * 2 + 0.1f));
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
            _random.NextFloat(-ExplosionScorchJitter, ExplosionScorchJitter),
            _random.NextFloat(-ExplosionScorchJitter, ExplosionScorchJitter));
        var brightness = _random.NextFloat(ExplosionScorchMinBrightness, 1f);
        var color = new Color(brightness, brightness, brightness, _random.NextFloat(ExplosionScorchMinAlpha, 1f));
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
            Log.Error($"Failed to get any decals for RMCScorchEffectComponent. Check that at least one decal has tag {decalTag}.");
    }

    private readonly record struct PendingScorch(TimeSpan At, EntityUid Grid, Vector2i Tile, float Power, float Density, bool Core);
}
