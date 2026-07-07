using Content.Shared.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Pneumotoxic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Damages the lungs. Prevents growth speed and lifespan mutations from occurring in plants.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var suppress = (float) potency * -2f;
        SuppressMutationSlot(plant, "Endurance", suppress);
        SuppressMutationSlot(plant, "Production", suppress);
        SuppressMutationSlot(plant, "Lifespan", suppress);
        SuppressMutationSlot(plant, "Maturity", suppress);
    }

    // TODO RMC14: mob effect - damage lung organ, oxygen damage on overdose/critical overdose
}
