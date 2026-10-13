using Content.Shared._RMC14.Particles;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Particles;

public sealed class RMCParticleEffectEventSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ParticleTileColorSystem _tileColor = default!;

    private static readonly ProtoId<ParticleEffectPrototype> ImpactSmoke = "RMCImpactSmoke";
    private static readonly ProtoId<ParticleEffectPrototype> ImpactFlash = "RMCImpactFlash";
    private static readonly ProtoId<ParticleEffectPrototype> BloodPuff = "RMCBloodSplatter";
    private static readonly ProtoId<ParticleEffectPrototype> BloodPuffEdge = "RMCBloodSplatterEdge";
    private static readonly ProtoId<ParticleEffectPrototype> BloodSpray = "RMCBloodSplatterSpray";

    private readonly Dictionary<NetEntity, List<ActiveEmitter>> _bySource = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RMCParticleEffectEvent>(OnParticleEffect);
        SubscribeAllEvent<RMCParticleEffectStopEvent>(OnParticleEffectStop);
        SubscribeNetworkEvent<RMCParticleEffectBatchEvent>(OnParticleEffectBatch);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeAllEvent<ProjectileImpactParticlesEvent>(OnProjectileImpacts);
    }

    private void OnParticleEffect(RMCParticleEffectEvent ev)
    {
        var color = ev.Color;
        if (ev.TileTint &&
            _tileColor.GetTile(ev.Coords) is { } tile &&
            _tileColor.GetTileColor(tile) is { } tileColor)
        {
            color = new Color(tileColor.R * ev.Brightness, tileColor.G * ev.Brightness, tileColor.B * ev.Brightness);
        }

        List<ActiveEmitter>? sourceEmitters = null;
        if (ev.Source is { } source)
        {
            sourceEmitters = _bySource.GetOrNew(source);
            sourceEmitters.RemoveAll(e => e.Exhausted);
        }

        foreach (var effect in ev.Effects)
        {
            var overrides = GetOverrides(ev);
            var emitter = ev.Direction is { } direction
                ? _particles.CreateParticle(effect, ev.Coords, direction, color, overrides)
                : _particles.CreateParticle(effect, ev.Coords, color, overrides);

            if (emitter != null)
                sourceEmitters?.Add(emitter);
        }
    }

    private void OnParticleEffectStop(RMCParticleEffectStopEvent ev)
    {
        StopSource(ev.Source);
    }

    private void OnParticleEffectBatch(RMCParticleEffectBatchEvent ev)
    {
        foreach (var source in ev.Stops)
        {
            StopSource(source);
        }

        foreach (var effect in ev.Effects)
        {
            OnParticleEffect(effect);
        }
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _bySource.Clear();
    }

    private void StopSource(NetEntity source)
    {
        if (!_bySource.Remove(source, out var emitters))
            return;

        foreach (var emitter in emitters)
        {
            emitter.Exhausted = true;
        }
    }

    private static ParticleRuntimeOverrides? GetOverrides(RMCParticleEffectEvent ev)
    {
        if (ev.Duration == null && ev.Radius == null && ev.Count == null && ev.Spread == null && ev.Intensity == null)
            return null;

        return new ParticleRuntimeOverrides
        {
            Duration = ev.Duration,
            ShapeRadius = ev.Radius,
            MaxCount = ev.Count,
            SpreadAngle = ev.Spread,
            Intensity = ev.Intensity,
        };
    }

    private void OnProjectileImpacts(ProjectileImpactParticlesEvent ev)
    {
        foreach (var impact in ev.Impacts)
        {
            var back = impact.Direction + Math.PI;
            if (impact.Projectile is { } projectile)
            {
                PlayProjectileImpact(impact, projectile, back);
                continue;
            }

            if (impact.Blood is { } blood)
            {
                var bloodOverrides = new ParticleRuntimeOverrides { Intensity = impact.Intensity };
                _particles.CreateParticle(BloodPuffEdge, impact.Coords, back, blood, bloodOverrides);
                _particles.CreateParticle(BloodPuff, impact.Coords, back, blood, bloodOverrides);
                var sprayOverrides = new ParticleRuntimeOverrides
                {
                    StartRotation = _particles.WorldToParticleRotation(impact.Direction),
                    Intensity = impact.Intensity,
                };
                _particles.CreateParticle(BloodSpray, impact.Coords, back, blood, sprayOverrides);
                continue;
            }

            var impactOverrides = new ParticleRuntimeOverrides { Intensity = impact.Intensity };
            _particles.CreateParticle(ImpactSmoke, impact.Coords, back, overrides: impactOverrides);
            _particles.CreateParticle(ImpactFlash, impact.Coords, back, overrides: impactOverrides);
            if (impact.Debris is { } debris)
                _particles.CreateParticle(debris, impact.Coords, back, overrides: impactOverrides);
        }
    }

    private void PlayProjectileImpact(ProjectileImpactParticles impact, EntProtoId projectile, Angle back)
    {
        if (!_prototype.TryIndex(projectile, out var proto) ||
            !proto.TryGetComponent(out ParticleProjectileImpactComponent? comp, _compFactory))
        {
            return;
        }

        foreach (var effect in comp.Effects)
        {
            _particles.CreateParticle(effect, impact.Coords, back, comp.Color);
        }

        if (impact.HitMob)
            return;

        foreach (var effect in comp.SurfaceEffects)
        {
            _particles.CreateParticle(effect, impact.Coords, back, comp.Color);
        }
    }
}
