using Content.Shared.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Nephrotoxic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Damages the kidneys. Prevents tolerance mutations from occurring in plants.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var suppress = (float) potency * -2f;
        SuppressMutationSlot(plant, "Light Tolerance", suppress);
        SuppressMutationSlot(plant, "Weed Tolerance", suppress);
        SuppressMutationSlot(plant, "Toxin Tolerance", suppress);
    }

    // TODO RMC14: mob effect - damage kidney organ, tox damage on overdose/critical overdose
}
