using Content.Shared._RMC14.Xenonids;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Crossmetabolizing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Can be metabolized in certain non-human species.";
    }

    protected override bool ShouldCancel(EntityEffectReagentArgs args)
    {
        return Potency < 2 && !args.EntityManager.HasComponent<XenoComponent>(args.TargetEntity);
    }
}
