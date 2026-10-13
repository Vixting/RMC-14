using Content.Shared._RMC14.Xenonids;
using Content.Shared.Destructible;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

public sealed class StructureDebrisParticleSystem : EntitySystem
{
    [Dependency] private readonly RMCParticleEffectSystem _particleEffects = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ParticleEffectPrototype> SlashSmoke = "RMCImpactSmoke";
    private static readonly ProtoId<ParticleEffectPrototype> DestructionDust = "RMCDestructionDust";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleDebrisComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<ParticleDebrisComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnAttacked(Entity<ParticleDebrisComponent> ent, ref AttackedEvent args)
    {
        if (ent.Comp.Effect is not { } debris || !HasComp<XenoComponent>(args.User))
            return;

        var coords = _transform.GetMapCoordinates(ent);
        var diff = _transform.GetWorldPosition(args.User) - coords.Position;
        Angle? direction = diff.LengthSquared() > 0.0001f ? diff.ToAngle() : null;
        _particleEffects.PlayPredicted(new RMCParticleEffectEvent(new List<ProtoId<ParticleEffectPrototype>> { debris, SlashSmoke }, coords, direction), args.User);
    }

    private void OnDestruction(Entity<ParticleDebrisComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.Effect is not { } debris)
            return;

        var coords = _transform.GetMapCoordinates(ent);
        _particleEffects.PlayPvs(new RMCParticleEffectEvent(new List<ProtoId<ParticleEffectPrototype>> { debris }, coords)
        {
            Count = ent.Comp.DestructionCount,
            Spread = Angle.FromDegrees(360),
            Intensity = ent.Comp.DestructionIntensity,
        });

        _particleEffects.PlayPvs(new RMCParticleEffectEvent(new List<ProtoId<ParticleEffectPrototype>> { DestructionDust }, coords));
    }
}
