using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Ciphering : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "This extremely complex chemical structure represents some kind of biological cipher.";
    }

    // TODO RMC14: reassigns an implanted xeno embryo to a specific hive
}
