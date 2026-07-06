using Content.Shared.Botany.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Trichogenic : RMCChemicalEffect
{
    [DataField]
    public float NutritionCost = 1f;

    [DataField]
    public float WaterCost = 1f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Increases plant yield at the cost of nutrients and water.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        plant.YieldMod += (int) MathF.Round((float) potency);
        plant.NutritionLevel -= NutritionCost;
        plant.WaterLevel -= WaterCost;
    }
}
