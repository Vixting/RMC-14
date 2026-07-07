using Content.Shared.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hepatotoxic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Damages the liver. Prevents certain negative mutations from occurring in plants.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var suppress = (float) potency * -2f;
        SuppressMutationSlot(plant, "Plant Cancer", suppress);
        SuppressMutationSlot(plant, "Gluttony", suppress);
    }

    // TODO RMC14: mob effect - damage liver organ, tox damage on overdose/critical overdose
}
