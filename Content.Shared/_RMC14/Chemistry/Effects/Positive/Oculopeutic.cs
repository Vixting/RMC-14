using Content.Shared.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Oculopeutic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Restores eyesight. Forces potency and cosmetic mutations to occur in plants.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        EnableMutationSlot(plant, "Potency", 1f);
        EnableMutationSlot(plant, "Bioluminescence", 1f);
        EnableMutationSlot(plant, "Flowers", 1f);
    }

    // TODO RMC14: mob effect - heal eyes organ damage and reduce blur/blindness, tox damage on overdose, brain + brute/burn damage on critical overdose
}
