using Content.Shared._RMC14.Temperature;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Cryometabolizing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Passively metabolized with no other effects at normal body temperature. Below freezing, metabolizes with increased effect.";
    }

    protected override void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        if (temperature.GetTemperature(args.TargetEntity) < 210f)
            boost += Potency * 0.25f;
    }
}
