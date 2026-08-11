using Content.Shared.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Fertilizing : RMCChemicalEffect
{
    [DataField]
    public float HealthMod = 0.8f;

    [DataField]
    public float YieldMod = 0.3f;

    [DataField]
    public float NutrientMod = 2f;

    [DataField]
    public float LifespanMod;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Acts as a potent fertilizer, feeding plants in a hydroponics tray while improving their health and yield.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (plant.Seed is not { } seed)
            return;

        var amount = (float) potency;
        plant.Health += amount * HealthMod;
        AddYieldMod(plant, amount * YieldMod);
        plant.NutritionLevel += amount * NutrientMod;

        if (LifespanMod == 0f)
            return;

        if (!seed.Unique)
            plant.Seed = seed = seed.Clone();

        seed.Lifespan += amount * LifespanMod;
    }
}
