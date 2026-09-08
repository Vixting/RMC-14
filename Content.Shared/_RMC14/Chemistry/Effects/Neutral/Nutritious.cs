using Content.Shared._RMC14.Botany;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Nutritious : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var updatedFactor = NutrimentFactor + Potency;
        return $"Restores [color=green]{updatedFactor * PotencyPerSecond}[/color] nutrients to the body and satiates hunger";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var scaled = (float) ActualPotency * 2f * (float) args.Quantity;
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        if (plant.Comp.Tray is { } trayUid && GetTray(args.EntityManager, plant) is { } tray)
        {
            plantTray.AdjustWeedLevel((trayUid, tray), scaled * 0.5f);
            plantTray.AdjustPestLevel((trayUid, tray), scaled * 0.5f);
            plantTray.AdjustNutritionLevel((trayUid, tray), scaled * 0.5f);
        }

        plantTray.AdjustHealth((plant.Owner, plant.Comp, null), scaled * 0.5f);
        AddYieldMod(args.EntityManager, plant, scaled * 0.05f);
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var mobState = args.EntityManager.System<MobStateSystem>();
        if (mobState.IsDead(args.TargetEntity))
            return;

        var hungerSys = args.EntityManager.System<HungerSystem>();
        var updatedFactor = NutrimentFactor + Potency;
        hungerSys.ModifyHunger(args.TargetEntity, updatedFactor * (float) potency);
    }
}
