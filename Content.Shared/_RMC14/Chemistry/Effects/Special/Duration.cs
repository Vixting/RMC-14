using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Duration : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Controls the duration of a chemical fire, using unknown means.";
    }

    public float DurationDelta => Potency;
    public FixedPoint2 DurationModDelta => 0.1f * Potency;
}
