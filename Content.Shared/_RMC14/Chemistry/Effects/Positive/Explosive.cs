using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Explosive : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Highly explosive. Do not ignite. Sensitivity is based on the overdose threshold, which can lead to spontaneous detonation.";
    }

    // TODO RMC14: makes the reagent holder explode when overdosed/ignited
}
