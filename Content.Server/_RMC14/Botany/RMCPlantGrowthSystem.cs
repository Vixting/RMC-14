using Content.Shared._RMC14.Botany;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Aging, maturation stage math, lifespan death, & production/harvest
/// </summary>
public sealed class RMCPlantGrowthSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public const float HydroponicsSpeedMultiplier = 1f;

    public int GetCurrentGrowthStage(Entity<RMCPlantComponent, RMCPlantGrowthComponent> plant)
    {
        var result = Math.Max(1, (int)(plant.Comp1.Age * plant.Comp2.GrowthStages / plant.Comp2.Maturation));
        return result;
    }

    /// <summary>
    /// Advances age. Runs early in the tick, before the nutrient/water/tolerance/toxin/pest/weed formuls
    /// </summary>
    public void TickAging(RMCPlantComponent comp)
    {
        if (comp.SkipAging > 0)
            comp.SkipAging--;
        else
        {
            if (_random.Prob(0.8f))
                comp.Age += (int)(1 * HydroponicsSpeedMultiplier);

            comp.UpdateSpriteAfterUpdate = true;
        }
    }

    /// <summary>
    /// Applies lifespan death and toggles the harvest-ready flag. Runs late in the tick, after the
    /// nutrient/water/tolerance/toxin/pest/weed formulas
    /// </summary>
    public bool TickLifespanAndProduction(Entity<RMCPlantComponent, RMCPlantGrowthComponent> plant)
    {
        var (_, comp, growth) = plant;

        if (comp.Age > growth.Lifespan)
        {
            comp.Health -= _random.Next(3, 5) * HydroponicsSpeedMultiplier;
            comp.UpdateSpriteAfterUpdate = true;
        }
        else if (comp.Age < 0)
        {
            return true;
        }

        if (TryComp(plant.Owner, out RMCPlantHarvestComponent? harvest) && harvest.ProductPrototypes.Count > 0)
        {
            if (comp.Age > growth.Production)
            {
                if (comp.Age - growth.LastProduce > growth.Production && !comp.Harvest)
                {
                    comp.Harvest = true;
                    growth.LastProduce = comp.Age;
                }
            }
            else if (comp.Harvest)
            {
                comp.Harvest = false;
                growth.LastProduce = comp.Age;
            }
        }

        return false;
    }

    /// <summary>
    /// Mutation  growth rate nudge: speeds up/slows down maturation or shifts the next production
    /// </summary>
    public void AffectGrowth(Entity<RMCPlantComponent, RMCPlantGrowthComponent> plant, int amount)
    {
        var (_, comp, growth) = plant;

        if (amount > 0)
        {
            if (comp.Age < growth.Maturation)
                comp.Age += amount;
            else if (!comp.Harvest && growth.Production <= 0f)
                growth.LastProduce -= amount;
        }
        else
        {
            if (comp.Age < growth.Maturation)
                comp.SkipAging++;
            else if (!comp.Harvest && growth.Production <= 0f)
                growth.LastProduce += amount;
        }
    }

    /// <summary>
    /// Decays MetabolismAdjust toward zero by 5 per tick. Called once per tray growth cycle.
    /// </summary>
    public void DecayMetabolismAdjust(RMCPlantComponent comp)
    {
        if (comp.MetabolismAdjust > 0)
            comp.MetabolismAdjust = MathF.Max(0f, comp.MetabolismAdjust - 5f);
        else if (comp.MetabolismAdjust < 0)
            comp.MetabolismAdjust = MathF.Min(0f, comp.MetabolismAdjust + 5f);
    }

    public void CheckLevelSanity(RMCPlantComponent comp, RMCPlantGrowthComponent growth)
    {
        comp.Health = MathHelper.Clamp(comp.Health, 0, growth.Endurance);
        comp.MutationLevel = MathHelper.Clamp(comp.MutationLevel, 0f, 100f);
        comp.YieldMod = MathHelper.Clamp(comp.YieldMod, 0, 2);
        comp.MutationMod = MathHelper.Clamp(comp.MutationMod, 0f, 3f);
    }
}
