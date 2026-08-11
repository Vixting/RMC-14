using Content.Shared.Botany.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects;

public abstract partial class RMCChemicalEffect : EntityEffect
{
    [DataField]
    public float Potency;

    private float _moddedPotency;

    /// <summary>
    ///     The value that should be used in actual calculations for chemical effect
    ///     Halved since potency is halved before being used
    /// </summary>
    public float ActualPotency => (_moddedPotency != 0 ? _moddedPotency : Potency) * 0.5f;

    // Halved again since chemicals tick every second in SS14, not every 2
    public float PotencyPerSecond => ActualPotency * 0.5f;

    [DataField]
    public float NutFactor;
    [DataField]
    public float NutMetabolism;

    public float NutrimentFactor => NutFactor * NutMetabolism;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs { Reagent: { } reagent } reagentArgs)
            return;

        var damageable = args.EntityManager.System<DamageableSystem>();
        var scale = reagentArgs.Scale;
        var boost = CalculateReagentBoost(reagentArgs);
        _moddedPotency = Potency + boost;
        var scaledPotency = PotencyPerSecond * scale;

        if (args.EntityManager.TryGetComponent<PlantHolderComponent>(args.TargetEntity, out var plant))
        {
            TickHydroTray(plant, scaledPotency, reagentArgs);
            return;
        }

        Tick(damageable, scaledPotency, reagentArgs);

        var totalQuantity = FixedPoint2.Zero;
        if (reagentArgs.Source != null)
            totalQuantity = reagentArgs.Source.GetTotalPrototypeQuantity(reagent.ID);

        if (reagent.Overdose != null && totalQuantity >= reagent.Overdose && !IsRegulated(reagentArgs))
            TickOverdose(damageable, scaledPotency, reagentArgs);

        if (reagent.CriticalOverdose != null && totalQuantity >= reagent.CriticalOverdose && !IsRegulated(reagentArgs))
            TickCriticalOverdose(damageable, scaledPotency, reagentArgs);
    }

    private static float CalculateReagentBoost(EntityEffectReagentArgs args)
    {
        var boost = 0f;
        if (args.Reagent?.Metabolisms == null)
            return boost;

        foreach (var (_, entry) in args.Reagent.Metabolisms)
        {
            foreach (var effect in entry.Effects)
            {
                if (effect is RMCChemicalEffect rmcEffect)
                {
                    rmcEffect.ReagentBoost(args, ref boost);
                }
            }
        }
        return boost;
    }

    private static bool IsRegulated(EntityEffectReagentArgs args)
    {
        if (args.Reagent?.Metabolisms == null)
            return false;

        foreach (var (_, entry) in args.Reagent.Metabolisms)
        {
            foreach (var effect in entry.Effects)
            {
                if (effect is Special.Regulating)
                    return true;
            }
        }

        return false;
    }

    protected virtual void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
    }

    protected virtual void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }

    protected static void AddYieldMod(PlantHolderComponent plant, float delta)
    {
        if (delta == 0f)
            return;

        var whole = (int) MathF.Floor(delta);
        var remainder = delta - whole;
        if (remainder > 0f && IoCManager.Resolve<IRobustRandom>().Prob(remainder))
            whole += 1;

        plant.YieldMod += whole;
    }

    protected static void SuppressMutationSlot(PlantHolderComponent plant, string slot, float value)
    {
        if (plant.Seed is not { } seed)
            return;

        if (seed.MutationController.GetValueOrDefault(slot, 0f) <= value)
            return;

        if (!seed.Unique)
            plant.Seed = seed = seed.Clone();

        seed.MutationController[slot] = value;
    }

    protected static void EnableMutationSlot(PlantHolderComponent plant, string slot, float value)
    {
        if (plant.Seed is not { } seed)
            return;

        if (seed.MutationController.GetValueOrDefault(slot, 0f) >= value)
            return;

        if (!seed.Unique)
            plant.Seed = seed = seed.Clone();

        seed.MutationController[slot] = value;
    }

    protected virtual void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }

    protected virtual void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }

    protected virtual void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }
}
