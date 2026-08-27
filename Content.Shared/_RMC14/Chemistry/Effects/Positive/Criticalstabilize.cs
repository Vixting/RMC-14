using Content.Shared._RMC14.Damage;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Criticalstabilize : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Stabilizes critical damage and bleeding.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        if (!entities.TryGetComponent<MobStateComponent>(args.TargetEntity, out var mobState) ||
            mobState.CurrentState != MobState.Critical)
            return;

        if (entities.TryGetComponent<DamageableComponent>(args.TargetEntity, out var damageableComp) &&
            entities.System<MobThresholdSystem>().TryGetIncapThreshold(args.TargetEntity, out var incapThreshold))
        {
            var prototypes = IoCManager.Resolve<IPrototypeManager>();
            var rmcDamageable = entities.System<SharedRMCDamageableSystem>();
            var heal = new DamageSpecifier();

            if (prototypes.TryIndex(BruteGroup, out var bruteGroup) &&
                damageableComp.Damage.TryGetDamageInGroup(bruteGroup, out var brute) &&
                brute >= incapThreshold.Value)
            {
                heal = rmcDamageable.DistributeHealingCached((args.TargetEntity, damageableComp), BruteGroup, 1, heal);
            }

            if (prototypes.TryIndex(BurnGroup, out var burnGroup) &&
                damageableComp.Damage.TryGetDamageInGroup(burnGroup, out var burn) &&
                burn >= incapThreshold.Value)
            {
                heal = rmcDamageable.DistributeHealingCached((args.TargetEntity, damageableComp), BurnGroup, 1, heal);
            }

            if (damageableComp.Damage.DamageDict.TryGetValue(AsphyxiationType, out var oxy) && oxy > 20)
                heal.DamageDict[AsphyxiationType] = -1;

            if (heal.DamageDict.Count > 0)
                damageable.TryChangeDamage(args.TargetEntity, heal, true, interruptsDoAfters: false);
        }

        if (entities.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
        {
            var bloodstreamSystem = entities.System<SharedBloodstreamSystem>();
            bloodstreamSystem.TryModifyBleedAmount((args.TargetEntity, bloodstream), -0.67f);
        }
    }
}
