using Content.Shared.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Blighting : RMCChemicalEffect
{
    [DataField]
    public float PestMod = 2f;

    [DataField]
    public float NutrientDrain = 2f;

    [DataField]
    public float MinProduction = 1f;

    [DataField]
    public float DivergeVolume = 30f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Infests a hydroponics tray with pests, starves its plants of nutrients, and slowly mutates their line into a worse producer.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (plant.Seed is not { } seed)
            return;

        var amount = (float) potency;
        plant.PestLevel += amount * PestMod;
        plant.NutritionLevel -= amount * NutrientDrain;

        if (seed.Immutable || seed.Production <= MinProduction)
            return;

        if (!IoCManager.Resolve<IRobustRandom>().Prob(amount / DivergeVolume))
            return;

        if (!seed.Unique)
            plant.Seed = seed = seed.Clone();

        seed.Production = MathF.Max(0f, seed.Production - 1f);
    }
}
