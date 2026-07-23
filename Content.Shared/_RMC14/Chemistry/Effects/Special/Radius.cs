using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Radius : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Controls the radius of a chemical fire, using unknown means.";
    }

    public float RadiusDelta => Potency;
    public FixedPoint2 RadiusModDelta => 0.1f * Potency;
}
