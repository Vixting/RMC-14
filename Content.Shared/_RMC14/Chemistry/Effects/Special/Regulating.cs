using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Regulating : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "This chemical regulates its own metabolization and can never cause an overdose.";
    }

    // TODO RMC14: suppresses overdose/critical overdose ticks entirely
}
