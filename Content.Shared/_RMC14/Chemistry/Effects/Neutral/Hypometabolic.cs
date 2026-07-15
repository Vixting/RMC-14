using Content.Shared.Botany.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hypometabolic : RMCChemicalEffect
{
    [DataField]
    public float Amount = 5f;

    [DataField]
    public float MaxAdjust = 20f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Slows plant growth cycle. In mobs, this chemical takes longer to metabolize, spreading its effects out over more time.";
    }

    protected override void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
        boost -= Potency * 0.35f;
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        plant.MetabolismAdjust = MathF.Min(plant.MetabolismAdjust + Amount, MaxAdjust);
    }
}
