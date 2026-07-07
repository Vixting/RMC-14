using Content.Shared.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Cardiopeutic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Treats heart damage. Forces new chemical mutations to occur in plants.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        EnableMutationSlot(plant, "New Chems", 1f);
        EnableMutationSlot(plant, "New Chems2", 1f);
        EnableMutationSlot(plant, "New Chems3", 1f);
    }

    // TODO RMC14: mob effect - heal heart organ damage, oxygen damage on overdose, pain on critical overdose
}
