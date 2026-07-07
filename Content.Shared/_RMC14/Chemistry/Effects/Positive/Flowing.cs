using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Flowing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "The opposite of viscous - tends to spill everywhere, expanding the radius of a chemical fire.";
    }

    // TODO RMC14: expands spilled chemical fire spread radius
}
