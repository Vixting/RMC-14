using Content.Shared._RMC14.Damage;
using Content.Shared.Botany;
using Content.Shared.Botany.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Crystallization : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Not safe to administer. Hardens the root structure of plants, improving survivability during repeat harvests.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BruteGroup, potency * 0.5f);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (plant.Seed is not { } seed || seed.HarvestRepeat == HarvestType.Repeat)
            return;

        var amount = (float) args.Quantity;
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

    // TODO RMC14: liver organ damage
}
