using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Fueling : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Can be burned as fuel, expanding the burn time of a chemical fire. On contact, makes a target more flammable.";
    }

    // TODO RMC14: expands spilled chemical fire burn duration, adds fire stacks on contact without igniting
}
