using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Flowing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "The opposite of viscous - tends to spill everywhere, expanding the radius of a chemical fire.";
    }

    public float RadiusDelta => 2f * Potency;
    public FixedPoint2 IntensityModDelta => -0.05f * Potency;
    public FixedPoint2 DurationModDelta => -0.05f * Potency;
    public FixedPoint2 RadiusModDelta => 0.05f * Potency;
}
