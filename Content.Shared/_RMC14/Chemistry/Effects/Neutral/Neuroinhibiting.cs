using Content.Shared._RMC14.Deafness;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Neuroinhibiting : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Inhibits neurological processes in the brain, which can result in sight, hearing, and speech impairments.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var p = (float) potency;
        var target = args.TargetEntity;
        var duration = TimeSpan.FromSeconds(2);

        // TODO RMC: Should be cured by surgeries instead of being status effects
        var status = args.EntityManager.System<StatusEffectsSystem>();

        if (p > 1f)
            status.TryAddStatusEffect<TemporaryBlindnessComponent>(target, TemporaryBlindnessSystem.BlindingStatusEffect, duration, true);

        if (p > 2f)
            args.EntityManager.System<SharedDeafnessSystem>().TryDeafen(target, duration, true, ignoreProtection: true);

        if (p > 3f)
            status.TryAddStatusEffect(target, "Muted", duration, true, "Muted");
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        args.EntityManager.System<SharedStutteringSystem>().DoStutter(args.TargetEntity, TimeSpan.FromSeconds(10), true);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
