using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Water/nutrient consumption ticks & soil solution reagent metabolism
/// </summary>
public sealed class RMCPlantMetabolismSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public const float HydroponicsSpeedMultiplier = 1f;
    public const float HydroponicsConsumptionMultiplier = 2f;

    /// <summary>
    /// Consumes nutrients/water
    /// </summary>
    public void Tick(EntityUid plant, RMCPlantTrayComponent tray)
    {
        if (!TryComp(plant, out RMCPlantMetabolismComponent? metabolism))
            return;

        if (metabolism.NutrientConsumption > 0 && tray.NutritionLevel > 0 && _random.Prob(0.75f))
        {
            tray.NutritionLevel -= MathF.Max(0f, metabolism.NutrientConsumption * HydroponicsSpeedMultiplier);
            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;
        }

        if (metabolism.WaterConsumption > 0 && tray.WaterLevel > 0 && _random.Prob(0.75f))
        {
            tray.WaterLevel -= MathF.Max(0f,
                metabolism.WaterConsumption * HydroponicsConsumptionMultiplier * HydroponicsSpeedMultiplier);
            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;
        }
    }

    public void AdjustNutrient(RMCPlantTrayComponent tray, float amount)
    {
        tray.NutritionLevel += amount;
    }

    public void AdjustWater(RMCPlantTrayComponent tray, float amount)
    {
        tray.WaterLevel += amount;

        // Water dilutes toxins
        if (amount > 0)
            tray.Toxins -= amount * 4f;
    }

    public void UpdateReagents(EntityUid trayUid, RMCPlantTrayComponent tray, EntityUid? plant)
    {
        if (!_solutionContainerSystem.ResolveSolution(trayUid, tray.SoilSolutionName, ref tray.SoilSolution, out var solution))
            return;

        if (plant == null || solution.Volume <= 0 || (TryComp(plant, out RMCPlantComponent? plantComp) && plantComp.MutationLevel >= 25))
            return;

        var amt = FixedPoint2.New(1);
        foreach (var entry in _solutionContainerSystem.RemoveEachReagent(tray.SoilSolution.Value, amt))
        {
            var reagentProto = _prototype.IndexReagent<ReagentPrototype>(entry.Reagent.Prototype);
            reagentProto.ReactionPlant(plant.Value, entry, solution);
        }
    }
}
