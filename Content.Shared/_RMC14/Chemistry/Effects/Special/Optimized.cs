using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Optimized : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "This chemical's molecule is structured differently, resulting in a more efficient synthesis process.";
    }

    // TODO RMC14: increases the yield of the chemical reaction that produces this reagent
}
