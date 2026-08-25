using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Drunk;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Nervestimulating : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Increases neuron communication speed, improving reaction time, awareness, and muscular control.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
        {
            HandleTouch(args);
            return;
        }

        var reduction = TimeSpan.FromSeconds((double) PotencyPerSecond);

        var oldStatus = args.EntityManager.System<StatusEffectsSystem>();
        oldStatus.TryRemoveTime(args.TargetEntity, "Stun", reduction);
        oldStatus.TryRemoveTime(args.TargetEntity, "KnockedDown", reduction);

        var stutter = args.EntityManager.System<SharedStutteringSystem>();
        stutter.DoRemoveStutterTime(args.TargetEntity, (double) PotencyPerSecond);

        if (!(ActualPotency > 2))
            return;

        var reductionSeconds = 2f * (float) potency;
        stutter.DoRemoveStutterTime(args.TargetEntity, reductionSeconds);

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var gradualReduction = TimeSpan.FromSeconds(-reductionSeconds);
        status.TryAddTime(args.TargetEntity, "RMCStatusEffectConfused", gradualReduction);
        status.TryAddTime(args.TargetEntity, "StatusEffectDrowsiness", gradualReduction);
        status.TryAddTime(args.TargetEntity, "Jitter", gradualReduction);

        var drunk = args.EntityManager.System<SharedDrunkSystem>();
        drunk.TryRemoveDrunkenessTime(args.TargetEntity, reductionSeconds);
    }

    private void HandleTouch(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var isXenoHuman = entities.HasComponent<XenoComponent>(target) ||
            (entities.HasComponent<HumanoidAppearanceComponent>(target) && !entities.HasComponent<SynthComponent>(target));
        if (!isXenoHuman || !(ActualPotency > 3))
            return;

        var oldStatus = entities.System<StatusEffectsSystem>();
        oldStatus.TryRemoveStatusEffect(target, "Stun");
        oldStatus.TryRemoveStatusEffect(target, "KnockedDown");

        var status = entities.System<SharedStatusEffectsSystem>();
        status.TryRemoveStatusEffect(target, "Dazed");
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damage.DamageDict[HeatType] = potency;
        damage.DamageDict[PoisonType] = potency * 3f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
