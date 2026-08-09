using Content.Shared.Botany.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Oculotoxic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Damages the eyes. Prevents potency and cosmetic mutations from occurring in plants.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var suppress = (float) potency * -2f;
        SuppressMutationSlot(plant, "Potency", suppress);
        SuppressMutationSlot(plant, "Bioluminescence", suppress);
        SuppressMutationSlot(plant, "Flowers", suppress);
    }

    // TODO RMC14: mob effect - damage eyes organ
    // TODO RMC14: mob effect - brain damage on critical overdose

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        args.EntityManager.System<BlindableSystem>().AdjustEyeDamage(args.TargetEntity, 8);
    }
}
