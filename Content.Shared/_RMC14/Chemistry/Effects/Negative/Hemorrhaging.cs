using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Botany.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hemorrhaging : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes hemorrhaging.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = 0.4f * Potency * (float) args.Quantity;
        plant.Health -= amount;
        plant.MutationMod += amount;
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
            return;

        var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();
        bloodstreamSystem.TryModifyBleedAmount((args.TargetEntity, bloodstream), (float) potency);
    }

    // TODO RMC14: - organ damage on overdose

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
            return;

        var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();
        bloodstreamSystem.TryModifyBleedAmount((args.TargetEntity, bloodstream), (float) potency * 4f);
    }
}
