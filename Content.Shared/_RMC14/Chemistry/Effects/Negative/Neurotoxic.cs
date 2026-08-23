using Content.Shared._RMC14.Slow;
using Content.Shared.Botany.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Neurotoxic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Damages the brain. Prevents species mutation from occurring in plants.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var suppress = (float) potency * -2f;
        SuppressMutationSlot(plant, "Mutate Species", suppress);
    }

    // TODO RMC14: brain damage

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14: brain damage on overdose

        var jitter = args.EntityManager.System<SharedJitteringSystem>();
        jitter.DoJitter(args.TargetEntity, TimeSpan.FromSeconds((float) potency), false);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.5f))
            return;

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddStatusEffectDuration(args.TargetEntity, "StatusEffectDrowsiness", TimeSpan.FromSeconds((float) potency));
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.15f * (float) potency))
            return;

        var slow = args.EntityManager.System<RMCSlowSystem>();
        slow.TrySuperSlowdown(args.TargetEntity, TimeSpan.FromSeconds((float) potency * 0.4f));
    }
}
