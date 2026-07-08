using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Encephalophrasive : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Drastically increases the amplitude of the host's brain waves, allowing them to broadcast their mind.";
    }

    // TODO RMC14: grants a psychic whisper action while active
}
