using Content.Shared._RMC14.Synth;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Encephalophrasive : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Drastically increases the amplitude of the host's brain waves, allowing them to broadcast their mind.";
    }

    // TODO RMC14: pain (naruto)

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
            return;

        var entities = args.EntityManager;
        var target = args.TargetEntity;
        if (!entities.HasComponent<HumanoidAppearanceComponent>(target) || entities.HasComponent<SynthComponent>(target))
            return;

        entities.System<EncephalophrasiveSystem>().Refresh(target);
    }

    // TODO RMC14: brain damage on overdose

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds(2), true);
    }
}
