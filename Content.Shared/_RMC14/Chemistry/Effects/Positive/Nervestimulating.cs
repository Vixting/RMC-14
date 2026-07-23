using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
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
        var reduction = TimeSpan.FromSeconds((double) PotencyPerSecond);

        var oldStatus = args.EntityManager.System<StatusEffectsSystem>();
        oldStatus.TryRemoveTime(args.TargetEntity, "Stun", reduction);
        oldStatus.TryRemoveTime(args.TargetEntity, "KnockedDown", reduction);

        var stutter = args.EntityManager.System<SharedStutteringSystem>();
        stutter.DoRemoveStutterTime(args.TargetEntity, (double) PotencyPerSecond);

        if (!(ActualPotency >= 3))
            return;

        stutter.DoRemoveStutter(args.TargetEntity, 0);

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryRemoveStatusEffect(args.TargetEntity, "Jitter");
        status.TryRemoveStatusEffect(args.TargetEntity, "StatusEffectDrowsiness");
        status.TryRemoveStatusEffect(args.TargetEntity, "Dazed");
        status.TryRemoveStatusEffect(args.TargetEntity, "StatusEffectConfused");

        args.EntityManager.System<BlindableSystem>().AdjustEyeDamage(args.TargetEntity, -9);
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
