using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Embryonic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Carries an infectious parasitic embryonic organism.";
    }

    // TODO RMC14: implants a xeno embryo in the target
}
