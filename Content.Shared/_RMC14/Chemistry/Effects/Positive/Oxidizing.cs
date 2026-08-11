using Content.Shared._RMC14.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry;
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

    public float IntensityDelta => 7f * Potency;
    public float DurationDelta => -4f * Potency;
    public FixedPoint2 IntensityModDelta => 0.2f * Potency;
    public FixedPoint2 DurationModDelta => -0.1f * Potency;
    public FixedPoint2 RadiusModDelta => -0.01f * Potency;

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<FlammableComponent>(args.TargetEntity, out var flammable))
            return;

        if (args.Method != ReactionMethod.Touch)
            return;

        var flammableSystem = args.EntityManager.System<SharedRMCFlammableSystem>();

        var boost = MathF.Max(flammable.FireStacks, (float) args.Quantity * ActualPotency);
        flammableSystem.AdjustStacks((args.TargetEntity, flammable), boost);

        if (ActualPotency <= 4f)
            return;

        flammableSystem.Ignite((args.TargetEntity, flammable), (int) ActualPotency, (int) boost, null);
    }
}
