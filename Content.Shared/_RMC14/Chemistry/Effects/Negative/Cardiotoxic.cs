using Content.Shared.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Cardiotoxic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Damages the heart. Prevents new chemical mutations from occurring in plants.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var suppress = (float) potency * -2f;
        SuppressMutationSlot(plant, "New Chems", suppress);
        SuppressMutationSlot(plant, "New Chems2", suppress);
        SuppressMutationSlot(plant, "New Chems3", suppress);
    }

    // TODO RMC14: mob effect - damage heart organ, oxygen damage on overdose/critical overdose
}
