using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Botany.Components;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
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
        var suppress = -Potency;
        SuppressMutationSlot(plant, "Mutate Species", suppress);
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
        {
            HandleTouch(args);
            return;
        }
    }

    private void HandleTouch(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var potency = (float) ActualPotency;
        var volume = (float) args.Quantity;
        if (potency <= 0f || volume <= 0f)
            return;

        var isXeno = entities.HasComponent<XenoComponent>(target);
        var isHuman = entities.HasComponent<HumanoidAppearanceComponent>(target) &&
            !entities.HasComponent<SynthComponent>(target);
        if (!isXeno && !isHuman)
            return;

        var dazed = entities.System<RMCDazedSystem>();

        if (isHuman)
            dazed.TryDaze(target, TimeSpan.FromSeconds(potency * volume * 0.25f), false);

        if (isXeno)
        {
            var duration = MathF.Min(potency * volume * 0.5f, 30f);
            dazed.TryDaze(target, TimeSpan.FromSeconds(duration), false);
        }
    }

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
