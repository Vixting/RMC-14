using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Viscous : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Thick and gooey due to high surface tension. Decreases the spread radius of a chemical fire.";
    }

    // TODO RMC14: reduces spilled chemical fire spread radius
}
