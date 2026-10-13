using Content.Shared._RMC14.Particles;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Particles;

public sealed class GibMistParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleBloodColorSystem _bloodColor = default!;
    [Dependency] private readonly RMCParticleEffectSystem _particleEffects = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ParticleEffectPrototype> Mist = "RMCGibBloodCloud";
    private static readonly ProtoId<ParticleEffectPrototype> Chunks = "RMCGibChunks";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, BeingGibbedEvent>(OnGibbed);
    }

    private void OnGibbed(Entity<BodyComponent> ent, ref BeingGibbedEvent args)
    {
        if (!_bloodColor.TryGetBloodColor(ent, out var color))
            color = Color.Red;

        var coords = _transform.GetMapCoordinates(ent);
        var ev = new RMCParticleEffectEvent(new List<ProtoId<ParticleEffectPrototype>> { Mist, Chunks }, coords, color: color);
        _particleEffects.PlayPvs(ev);
    }
}
