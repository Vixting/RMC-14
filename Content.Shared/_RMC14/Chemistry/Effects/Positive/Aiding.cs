using Content.Shared._RMC14.Movement;
using Content.Shared.Botany.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Aiding : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";
    private static readonly ProtoId<DamageTypePrototype> CellularType = "Cellular";

    private static readonly EntProtoId ConfusedStatus = "RMCStatusEffectConfused";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Removes compounds modifying yield and mutation in plants.";
    }

    // TODO RMC14: mob effect - cures disabilities

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        plant.MutationMod -= (float) potency;
        plant.YieldMod -= (int) MathF.Round((float) potency);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TrySetStatusEffectDuration(args.TargetEntity, ConfusedStatus, TimeSpan.FromSeconds((float) potency * 2f));

        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damage.DamageDict[CellularType] = potency * 3f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds((float) potency * 2f), true);
    }
}
