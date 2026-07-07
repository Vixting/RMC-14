using Content.Shared.Botany;
using Content.Shared.Botany.Components;
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

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (plant.Seed is not { } seed || seed.HarvestRepeat == HarvestType.Repeat)
            return;

        var amount = (float) potency;
        plant.WeedLevel += amount * 0.25f;
        plant.NutritionLevel -= amount * 0.25f;
        plant.RepeatHarvestCounter += amount * 10f;

        if (plant.RepeatHarvestCounter < 100f)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.5f))
        {
            plant.RepeatHarvestCounter -= random.Next(20, 51);
            return;
        }

        if (!seed.Unique)
            plant.Seed = seed = seed.Clone();

        seed.HarvestRepeat = HarvestType.Repeat;
        plant.RepeatHarvestCounter = 0f;

        var popup = args.EntityManager.System<SharedPopupSystem>();
        popup.PopupEntity(Loc.GetString("plant-repeat-harvest-shimmer", ("name", Loc.GetString(seed.DisplayName))), args.TargetEntity);
    }

    // TODO RMC14: mob effect - migraine popup and brain organ damage
}
