using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared._RMC14.Damage;
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
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageTypePrototype> CellularType = "Cellular";

    private static readonly EntProtoId ConfusedStatus = "RMCStatusEffectConfused";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Removes compounds modifying yield and mutation in plants.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        RMCDisabilities.ClearAll(args.EntityManager, args.TargetEntity);
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var scaled = (float) ActualPotency * 2f * (float) args.Quantity;
        plant.MutationMod -= 4f * scaled;
        plant.YieldMod -= (int) MathF.Round(4f * scaled);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TrySetStatusEffectDuration(args.TargetEntity, ConfusedStatus, TimeSpan.FromSeconds((float) potency * 2f));

        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency);
        damage.DamageDict[CellularType] = potency * 3f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds((float) potency * 2f), true);
    }
}
