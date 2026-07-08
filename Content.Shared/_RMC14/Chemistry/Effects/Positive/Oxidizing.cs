using Content.Shared._RMC14.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Oxidizing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Increases the intensity of chemical fires, at the cost of burning fuel faster. On contact with a high enough potency, ignites the target.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if ((float) potency <= 4f)
            return;

        if (!args.EntityManager.TryGetComponent<FlammableComponent>(args.TargetEntity, out var flammable))
            return;

        var flammableSystem = args.EntityManager.System<SharedRMCFlammableSystem>();
        flammableSystem.Ignite((args.TargetEntity, flammable), (int) potency, 10, null);
    }

    // TODO RMC14: increases spilled chemical fire intensity, adds fire stacks on contact below the ignition threshold
}
