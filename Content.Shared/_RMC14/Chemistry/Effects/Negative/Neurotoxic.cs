using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Botany.Components;
using Content.Shared.Chemistry;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Neurotoxic : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private static readonly EntProtoId ResistNeuroStatus = "RMCStatusEffectResistNeuro";

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

        // TODO RMC14: baseline brain damage
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

        var entities = args.EntityManager;
        var target = args.TargetEntity;

        // cm13 apply_neuro(): xenomorphs are immune to this effect entirely
        if (entities.HasComponent<XenoComponent>(target))
            return;

        // cm13 apply_neuro(): a CHEM_EFFECT_RESIST_NEURO source (Neuroshielding) shrugs the effect off
        var status = entities.System<SharedStatusEffectsSystem>();
        if (status.HasStatusEffect(target, ResistNeuroStatus))
            return;

        var slow = entities.System<RMCSlowSystem>();
        var duration = TimeSpan.FromSeconds((float) potency * 0.4f);
        slow.TrySuperSlowdown(target, duration);

        // cm13: also falls prone (knockdown+stun) if wearing no outer clothing, or clothing that grants no slowdown
        var inventory = entities.System<InventorySystem>();
        var armored = inventory.TryGetSlotEntity(target, "outerClothing", out var outer) &&
            entities.TryGetComponent<ClothingSpeedModifierComponent>(outer, out var mod) &&
            (mod.WalkModifier != 1f || mod.SprintModifier != 1f);

        if (armored)
            return;

        var stun = entities.System<SharedStunSystem>();
        stun.TryKnockdown(target, duration, true);
        stun.TryStun(target, duration, true);
    }
}
