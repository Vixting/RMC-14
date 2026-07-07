using Content.Shared.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Aiding : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Removes compounds modifying yield and mutation in plants.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        plant.MutationMod -= (float) potency;
        plant.YieldMod -= (int) MathF.Round((float) potency);
    }

    // TODO RMC14: mob effect - cures disabilities, confusion + tox damage on overdose, paralyze + tox + clone damage on critical overdose
}
