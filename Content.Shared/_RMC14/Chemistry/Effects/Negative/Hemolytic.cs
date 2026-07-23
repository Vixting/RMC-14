using Content.Shared._RMC14.Slow;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hemolytic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes intravascular hemolysis, destroying erythrocytes in the bloodstream.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
        {
            var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();
            bloodstreamSystem.TryModifyBloodLevel((args.TargetEntity, bloodstream), -potency * 5f);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
        {
            var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();
            bloodstreamSystem.TryModifyBloodLevel((args.TargetEntity, bloodstream), -potency * 8f);
        }

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddStatusEffectDuration(args.TargetEntity, "StatusEffectDrowsiness", TimeSpan.FromSeconds((double) potency));

        var slow = args.EntityManager.System<RMCSlowSystem>();
        slow.TrySuperSlowdown(args.TargetEntity, TimeSpan.FromSeconds(2));
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = potency * 5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
