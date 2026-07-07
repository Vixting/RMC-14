using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Ravening : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Carries the X-65 biological organism."; // TODO RMC14 rename I think
    }

    // TODO RMC14: infects the host with a zombifying disease
}
