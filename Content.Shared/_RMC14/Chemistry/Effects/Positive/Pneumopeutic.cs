using Content.Shared.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Pneumopeutic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Treats lung damage. Forces growth speed and lifespan mutations to occur in plants.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        EnableMutationSlot(plant, "Endurance", 1f);
        EnableMutationSlot(plant, "Production", 1f);
        EnableMutationSlot(plant, "Lifespan", 1f);
        EnableMutationSlot(plant, "Maturity", 1f);
    }

    // TODO RMC14: mob effect - heal lung organ damage, damages lungs on overdose, oxygen damage on critical overdose
}
