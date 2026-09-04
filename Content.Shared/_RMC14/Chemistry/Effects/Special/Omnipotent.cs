using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Chemistry.Disabilities;
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

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = Potency * (float) args.Quantity;

        if (GetTray(args.EntityManager, plant) is { } tray)
        {
            tray.NutritionLevel += amount * 0.5f;
            tray.WeedLevel -= amount * 2.5f;
            tray.PestLevel -= amount * 2.5f;
        }

        plant.Comp.Health += amount;
        plant.Comp.YieldMod += (int) MathF.Round(amount);
        plant.Comp.MutationMod += amount;
    }

    // TODO RMC14: fully heals damage, cures diseases, clears stuns/confusion/jitteriness
}
