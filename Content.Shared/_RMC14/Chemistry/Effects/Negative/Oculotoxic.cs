using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared.Botany.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
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
        var suppress = -Potency;
        SuppressMutationSlot(plant, "Potency", suppress);
        SuppressMutationSlot(plant, "Bioluminescence", suppress);
        SuppressMutationSlot(plant, "Flowers", suppress);
    }

    // TODO RMC14: damage eyes
    // TODO RMC14: brain damage on critical overdose

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        args.EntityManager.EnsureComponent<RMCBlindDisabilityComponent>(args.TargetEntity);
    }
}
