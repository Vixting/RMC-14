using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Viscous : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Thick and gooey due to high surface tension. Decreases the spread radius of a chemical fire.";
    }

    // potency = level*LEVEL_TO_POTENCY_MULTIPLIER(0.5)), so this applies directly with no extra scaling
    public FixedPoint2 RadiusModDelta => -0.025f * Potency;
}
