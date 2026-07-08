using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class CipheringPredator : RMCChemicalEffect // TODO RMC14: Rename / remove maybe
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "This extremely complex chemical structure represents a predator-strain biological cipher.";
    }

    // TODO RMC14: mutates an alien egg into a predator-strain hive egg
