using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Generation;

public sealed partial class RMCReactionIndicatorEffect : EventEntityEffect<RMCReactionIndicatorEffect>
{
    [DataField(required: true)]
    public ReactionIndicator Indicator;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }
}
