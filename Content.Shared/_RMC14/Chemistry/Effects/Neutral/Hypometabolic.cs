using Content.Shared._RMC14.Botany;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hypometabolic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Slows plant growth cycle. In mobs, this chemical takes longer to metabolize, spreading its effects out over more time.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var delta = Math.Clamp(20f * (float) ActualPotency, 0f, 130f);
        plant.Comp.MetabolismAdjust = MathF.Min(plant.Comp.MetabolismAdjust + delta, 130f);
    }
}
