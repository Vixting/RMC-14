using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Duration : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Controls the duration of a chemical fire, using unknown means.";
    }

    // TODO RMC14: modifies spilled chemical fire burn duration
}
