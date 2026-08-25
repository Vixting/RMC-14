using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Intravenous : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Due to its chemical composition, this can only be administered intravenously.";
    }

    protected override void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
        boost += Potency;
    }

    public override float MetabolismRateMultiplier => Potency;

    public static bool IsNotIngestible(ReagentPrototype reagent)
    {
        if (reagent.Metabolisms == null)
            return false;

        foreach (var (_, entry) in reagent.Metabolisms)
        {
            foreach (var effect in entry.Effects)
            {
                if (effect is Intravenous)
                    return true;
            }
        }

        return false;
    }
}
