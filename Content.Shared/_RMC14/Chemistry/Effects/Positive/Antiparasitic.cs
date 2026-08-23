using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Antiparasitic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";
    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";
    private const float HostDamageMultiplier = 1.5f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Antimicrobial property specifically targeting parasitic pathogens, disrupting their growth and " +
            "potentially killing them. Causes minor burns to the host, and can cure an infection outright with " +
            "sustained treatment.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<VictimInfectedComponent>(args.TargetEntity, out var infected))
            return;

        var parasiteSystem = args.EntityManager.System<SharedXenoParasiteSystem>();
        var progress = TimeSpan.FromSeconds((double) potency);
        parasiteSystem.DelayBurst((args.TargetEntity, infected), progress);

        if (parasiteSystem.TryResistInfection((args.TargetEntity, infected), progress))
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict[HeatType] = potency * HostDamageMultiplier;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        args.EntityManager.RemoveComponent<VictimInfectedComponent>(args.TargetEntity);
    }
}
