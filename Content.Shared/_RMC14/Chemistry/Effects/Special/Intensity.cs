using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Intensity : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Controls the intensity of a chemical fire, using unknown means.";
    }

    public float IntensityDelta => Potency;
    public FixedPoint2 IntensityModDelta => 0.1f * Potency;
}
