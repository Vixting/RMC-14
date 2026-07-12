using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hallucinogenic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "StatusEffectSeeingRainbow";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes perception-like experiences without an external stimulus, such as hallucinations.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddStatusEffectDuration(args.TargetEntity, SeeingRainbows, TimeSpan.FromSeconds((float) potency));

        if ((float) potency > 1f)
        {
            var jitter = args.EntityManager.System<SharedJitteringSystem>();
            jitter.DoJitter(args.TargetEntity, TimeSpan.FromSeconds(2), false);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddStatusEffectDuration(args.TargetEntity, SeeingRainbows, TimeSpan.FromSeconds(10));

        var jitter = args.EntityManager.System<SharedJitteringSystem>();
        jitter.DoJitter(args.TargetEntity, TimeSpan.FromSeconds(2), false);

        // TODO RMC14: mob effect - stumble in a random direction
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds(2), true);
    }
}
