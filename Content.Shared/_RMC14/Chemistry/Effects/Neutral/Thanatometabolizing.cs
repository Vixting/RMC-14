using Content.Shared.EntityEffects;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Thanatometabolizing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Requires either low oxygen levels or low blood flow to function. Potency affects the efficiency of other properties in the mix.";
    }

    protected override void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
        if (args.EntityManager.TryGetComponent<MobStateComponent>(args.TargetEntity, out var mobState) &&
            mobState.CurrentState != MobState.Alive)
        {
            boost += Potency;
        }
    }
}
