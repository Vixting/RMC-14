using Content.Shared._RMC14.Chemistry.Buildup;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Hypergenetic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";
    private static readonly ProtoId<DamageTypePrototype> CellularType = "Cellular";

    private const float HealingReductionDissipation = 0.4f;
    private const float HealingReductionMax = 50f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Regenerates all types of cell membranes, mending damage in all organs and limbs.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
        {
            HandleTouch(damageable, args);
            return;
        }

        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = -potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    private void HandleTouch(DamageableSystem damageable, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var isXeno = entities.HasComponent<XenoComponent>(target);
        var isHuman = entities.HasComponent<HumanoidAppearanceComponent>(target) && !entities.HasComponent<SynthComponent>(target);
        if (!isXeno && !isHuman)
            return;

        var potency = (float) ActualPotency;
        var volume = (float) args.Quantity;

        var reduction = entities.System<RMCHealingReductionSystem>();
        reduction.AddBuildup(target, -potency * volume * 0.5f, HealingReductionDissipation, HealingReductionMax);

        var damage = new DamageSpecifier();
        if (isHuman)
            damage.DamageDict[BluntType] = -(potency * volume * 0.5f);

        if (isXeno)
        {
            var heal = potency * volume;
            damage.DamageDict[BluntType] = -heal;
            damage.DamageDict[HeatType] = -heal;
        }

        damageable.TryChangeDamage(target, damage, true, interruptsDoAfters: false);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[CellularType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency * 3f;
        damage.DamageDict[HeatType] = potency * 3f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
