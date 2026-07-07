using Content.Shared.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Hepatopeutic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Treats liver damage. Forces certain negative mutations to occur in plants.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        EnableMutationSlot(plant, "Plant Cancer", 1f);
        EnableMutationSlot(plant, "Gluttony", 1f);
    }

    // TODO RMC14: mob effect - heal liver organ damage, damages liver on overdose, tox damage on critical overdose
}
