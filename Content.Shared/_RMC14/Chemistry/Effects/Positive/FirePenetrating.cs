using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class FirePenetrating : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Gives the chemical an anomalous combustion chemistry, causing its flame to burn through flame-resistant material.";
    }

    // TODO RMC14: lets a chemical fire ignore fire resistance
}
