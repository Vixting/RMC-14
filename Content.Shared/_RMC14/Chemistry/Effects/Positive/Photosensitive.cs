using Content.Shared._RMC14.Botany;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Photosensitive : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Not safe to administer. Supercharges photosynthesis, treated plants may become able to be harvested repeatedly.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<RMCPlantHarvestComponent>(plant.Owner, out var harvest) || harvest.HarvestRepeat == HarvestType.Repeat)
            return;

        var amount = (float) ActualPotency * 2f * (float) args.Quantity;
        if (GetTray(args.EntityManager, plant) is { } tray)
        {
            tray.WeedLevel += amount * 0.25f;
            tray.NutritionLevel -= amount * 0.25f;
        }

        harvest.RepeatHarvestCounter += amount * 10f;

        if (harvest.RepeatHarvestCounter < 100f)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.5f))
        {
            harvest.RepeatHarvestCounter -= random.Next(20, 51);
            return;
        }

        harvest.HarvestRepeat = HarvestType.Repeat;
        harvest.RepeatHarvestCounter = 0f;

        if (args.EntityManager.TryGetComponent<RMCPlantChemicalsComponent>(plant.Owner, out var chemicals))
            chemicals.PotencyCounter = 0f;

        var popup = args.EntityManager.System<SharedPopupSystem>();
        popup.PopupEntity(Loc.GetString("plant-repeat-harvest-shimmer", ("name", Loc.GetString(plant.Comp.DisplayName))), args.TargetEntity);
    }

    // TODO RMC14: migraine popup & brain organ damage
}
