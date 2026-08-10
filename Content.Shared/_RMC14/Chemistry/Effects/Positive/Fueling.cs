using Content.Shared._RMC14.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Fueling : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Can be burned as fuel, expanding the burn time of a chemical fire. On contact, makes a target more flammable.";
    }

    public float IntensityDelta => -3f * Potency;
    public float DurationDelta => 7f * Potency;
    public FixedPoint2 IntensityModDelta => -0.1f * Potency;
    public FixedPoint2 DurationModDelta => 0.2f * Potency;
    public FixedPoint2 RadiusModDelta => 0.01f * Potency;

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method != ReactionMethod.Touch)
            return;

        if (!args.EntityManager.TryGetComponent<FlammableComponent>(args.TargetEntity, out var flammable))
            return;

        var flammableSystem = args.EntityManager.System<SharedRMCFlammableSystem>();
        flammableSystem.AdjustStacks((args.TargetEntity, flammable), (float) args.Quantity * ActualPotency * 0.5f);
    }
}
