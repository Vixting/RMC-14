using Content.Shared._RMC14.Botany;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hypermetabolic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Accelerates plant growth cycle. In mobs, speeds up this chemical's own metabolism, increasing overdose risk.";
    }


    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var delta = Math.Clamp(-20f * (float) ActualPotency, -130f, 0f);
        plant.Comp.MetabolismAdjust = MathF.Max(plant.Comp.MetabolismAdjust + delta, -130f);
    }
}
