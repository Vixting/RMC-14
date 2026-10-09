using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Particles;
using Content.Shared._RMC14.Water;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Particles;

/// <summary>
/// Plays an explosion types particle set at the epicenter of its explosions
/// </summary>
public sealed class ExplosionParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ParticleTileColorSystem _tileColor = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCWaterSystem _water = default!;

    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExplosionVisualsComponent, ComponentStartup>(OnExplosionStartup);
        SubscribeLocalEvent<ParticleExplosionOnSpawnComponent, ComponentStartup>(OnSpawnStartup);
    }

    private void OnExplosionStartup(Entity<ExplosionVisualsComponent> ent, ref ComponentStartup args)
    {
        if (!_prototype.TryIndex<ExplosionPrototype>(ent.Comp.ExplosionType, out var type) ||
            type.Particles is not { } particlesId ||
            !_prototype.TryIndex(particlesId, out var particles) ||
            ent.Comp.Intensity.Count == 0)
        {
            return;
        }

        if (ent.Comp.RMCStartTime > TimeSpan.Zero && _timing.CurTime - ent.Comp.RMCStartTime > MaxAge)
            return;

        var power = RMCExplosionVisuals.EpicenterPower(ent.Comp.Intensity, type.PowerPerIntensity);
        var tier = particles.GetTier(power, ent.Comp.RMCTinyParticles);
        PlayTier(tier, ent.Comp.Epicenter);
    }

    private void OnSpawnStartup(Entity<ParticleExplosionOnSpawnComponent> ent, ref ComponentStartup args)
    {
        if (!_random.Prob(ent.Comp.Probability) ||
            !_prototype.TryIndex(ent.Comp.Particles, out var particles))
        {
            return;
        }

        PlayTier(particles.Tiny, _transform.GetMapCoordinates(ent));
    }

    private void PlayTier(ExplosionParticleTier tier, MapCoordinates coords)
    {
        var water = IsWater(coords);
        var tile = _tileColor.GetTile(coords);
        var effects = tier.Effects;
        var surface = false;
        if (water && tier.WaterEffects.Count > 0)
        {
            effects = tier.WaterEffects;
        }
        else if (!water && tile is { } surfaceTile && TryGetSurface(tier, surfaceTile, out var match))
        {
            effects = match.Effects;
            surface = true;
        }

        foreach (var effect in effects)
        {
            _particles.CreateParticle(effect, coords);
        }

        if (!water && !surface)
        {
            var tileColor = tile is { } colorTile ? _tileColor.GetTileColor(colorTile) : null;
            foreach (var tinted in tier.TileColorEffects)
            {
                var color = tileColor is { } c
                    ? new Color(c.R * tinted.Brightness, c.G * tinted.Brightness, c.B * tinted.Brightness)
                    : tinted.Fallback;
                _particles.CreateParticle(tinted.Effect, coords, color);
            }
        }

        if (!water && tier.Flash is { } flash)
            Spawn(flash, coords, rotation: _random.NextAngle());
    }

    private bool TryGetSurface(ExplosionParticleTier tier, Tile tile, [NotNullWhen(true)] out ExplosionParticleSurface? surface)
    {
        surface = null;
        var tileId = _tileDefinition[tile.TypeId].ID;
        foreach (var candidate in tier.Surfaces)
        {
            foreach (var surfaceTile in candidate.Tiles)
            {
                if (surfaceTile.Id != tileId)
                    continue;

                surface = candidate;
                return true;
            }
        }

        return false;
    }

    private bool IsWater(MapCoordinates coords)
    {
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(coords);
        while (anchored.MoveNext(out var uid))
        {
            if (TryComp(uid, out RMCWaterComponent? water) && _water.CanCollide((uid, water), uid))
                return true;
        }

        return false;
    }
}
