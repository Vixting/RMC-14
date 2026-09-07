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
        if (GetTray(args.EntityManager, plant) is { } tray)
        {
            tray.WeedLevel += scaled * 0.5f;
            tray.PestLevel += scaled * 0.5f;
            tray.NutritionLevel += scaled * 0.5f;
        }

        plant.Comp.Health += scaled * 0.5f;
        AddYieldMod(plant, scaled * 0.05f);
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
