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
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();

        if (plant.Comp.Tray is { } trayUid && GetTray(args.EntityManager, plant) is { } tray)
        {
            plantTray.AdjustNutritionLevel((trayUid, tray), amount * 0.5f);
            plantTray.AdjustWeedLevel((trayUid, tray), -amount * 2.5f);
            plantTray.AdjustPestLevel((trayUid, tray), -amount * 2.5f);
        }

        plantTray.AdjustHealth((plant.Owner, plant.Comp, null), amount);
        plantTray.SetYieldMod((plant.Owner, plant.Comp), plant.Comp.YieldMod + (int) MathF.Round(amount));
        plantTray.AdjustMutationMod((plant.Owner, plant.Comp), amount);
    }

    // TODO RMC14: fully heals damage, cures diseases, clears stuns/confusion/jitteriness
}
