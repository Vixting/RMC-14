using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared.Botany.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Omnipotent : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Fully revitalizes all bodily functions.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        RMCDisabilities.ClearAll(args.EntityManager, args.TargetEntity);
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = (float) potency;
        plant.NutritionLevel += amount * 0.5f;
        plant.WeedLevel -= amount * 2.5f;
        plant.PestLevel -= amount * 2.5f;
        plant.Health += amount;
        plant.YieldMod += (int) MathF.Round(amount);
        plant.MutationMod += amount;
    }

    // TODO RMC14: fully heals damage, cures diseases, clears blindness/stuns/confusion/jitteriness
}
