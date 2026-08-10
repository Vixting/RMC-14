using Content.Shared._RMC14.Temperature;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Neurocryogenic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes a temporal freeze of neurological processes in the brain, preserving it for long periods of time.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (entities.TryGetComponent(target, out MobStateComponent? mobStateComp) &&
            entities.System<MobStateSystem>().IsDead(target, mobStateComp))
        {
            var rotting = entities.System<SharedRottingSystem>();
            rotting.ReduceAccumulator(target, TimeSpan.FromSeconds((double) potency));
            return;
        }

        var stun = entities.System<SharedStunSystem>();
        stun.TryParalyze(target, TimeSpan.FromSeconds(1), true);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        temperature.ForceChangeTemperature(args.TargetEntity, temperature.GetTemperature(args.TargetEntity) - (float) potency * 2.5f);
    }
}
