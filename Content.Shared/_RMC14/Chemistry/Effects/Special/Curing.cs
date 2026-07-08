using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Curing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Binds to and neutralizes specific microbiological organisms.";
    }

    // TODO RMC14: cures diseases such as the zombie infection
}
