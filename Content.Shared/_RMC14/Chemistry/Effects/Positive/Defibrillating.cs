using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Traits.Assorted;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Defibrillating : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";
    private static readonly ProtoId<DamageTypePrototype> CellularType = "Cellular";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes an electrochemical reaction in the cardiac muscles, forcing the heart to continue pumping. May cause irregular heart rhythms.";
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    // TODO RMC14: mob effect - heart organ damage on critical overdose

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (!entities.TryGetComponent(target, out MobStateComponent? mobStateComp))
            return;

        var mobState = entities.System<MobStateSystem>();
        if (!mobState.IsDead(target, mobStateComp))
        {
            entities.RemoveComponent<RMCDefibrillatingReviveComponent>(target);
            return;
        }

        var rotting = entities.System<SharedRottingSystem>();
        if (rotting.IsRotten(target) || entities.HasComponent<UnrevivableComponent>(target))
            return;

        var mobThreshold = entities.System<MobThresholdSystem>();
        if (!mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var deadThreshold) ||
            !entities.TryGetComponent(target, out DamageableComponent? damageableComp))
        {
            return;
        }

        var timing = IoCManager.Resolve<IGameTiming>();

        if (entities.TryGetComponent(target, out RMCDefibrillatingReviveComponent? pending))
        {
            if (damageableComp.TotalDamage >= deadThreshold)
                entities.RemoveComponent<RMCDefibrillatingReviveComponent>(target);
            else if (timing.CurTime >= pending.RevivesAt)
            {
                entities.RemoveComponent<RMCDefibrillatingReviveComponent>(target);
                mobState.ChangeMobState(target, MobState.Critical, mobStateComp);
            }

            return;
        }

        if (damageableComp.TotalDamage >= deadThreshold)
            ZapWithElectrogenetic(damageable, args, target);

        if (damageableComp.TotalDamage < deadThreshold)
        {
            var revive = entities.EnsureComponent<RMCDefibrillatingReviveComponent>(target);
            revive.RevivesAt = timing.CurTime + TimeSpan.FromSeconds(5);
            return;
        }

        if (ActualPotency < 1)
            return;

        var heal = new DamageSpecifier();
        heal.DamageDict[BluntType] = -potency * 0.1f;
        heal.DamageDict[HeatType] = -potency * 0.1f;
        heal.DamageDict[PoisonType] = -potency * 0.1f;
        heal.DamageDict[CellularType] = -potency * 0.1f;

        if (ActualPotency > 2)
        {
            heal.DamageDict[BluntType] -= potency * 0.25f;
            heal.DamageDict[HeatType] -= potency * 0.25f;
            heal.DamageDict[PoisonType] -= potency * 0.25f;
            heal.DamageDict[CellularType] -= potency * 0.25f;
        }

        if (damageableComp.Damage.DamageDict.TryGetValue(AsphyxiationType, out var oxyLoss) && oxyLoss > FixedPoint2.Zero)
            heal.DamageDict[AsphyxiationType] = -oxyLoss;

        damageable.TryChangeDamage(target, heal, true, interruptsDoAfters: false);
    }

    private static void ZapWithElectrogenetic(DamageableSystem damageable, EntityEffectReagentArgs args, EntityUid target)
    {
        var entities = args.EntityManager;
        var bloodstream = entities.System<SharedRMCBloodstreamSystem>();
        if (!bloodstream.TryGetChemicalSolution(target, out var solutionEnt, out var solution))
            return;

        var rmcReagent = entities.System<RMCReagentSystem>();
        var solutionContainer = entities.System<SharedSolutionContainerSystem>();
        foreach (var quantity in solution.Contents)
        {
            if (!rmcReagent.TryIndex(quantity.Reagent.Prototype, out var reagent) ||
                reagent.Metabolisms == null)
            {
                continue;
            }

            Electrogenetic? electrogenetic = null;
            foreach (var (_, entry) in reagent.Metabolisms)
            {
                foreach (var effect in entry.Effects)
                {
                    if (effect is Electrogenetic found)
                        electrogenetic = found;
                }
            }

            if (electrogenetic == null)
                continue;

            var heal = electrogenetic.CalculateHeal(damageable, target, entities);
            damageable.TryChangeDamage(target, heal, true, interruptsDoAfters: false);
            solutionContainer.RemoveReagent(solutionEnt, quantity.Reagent.Prototype, 1);
            break;
        }
    }
}

[RegisterComponent]
public sealed partial class RMCDefibrillatingReviveComponent : Component
{
    [DataField]
    public TimeSpan RevivesAt;
}
