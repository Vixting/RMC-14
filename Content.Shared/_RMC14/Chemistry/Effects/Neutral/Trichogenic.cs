using System.Linq;
using Content.Shared.Botany.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Popups;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Trichogenic : RMCChemicalEffect
{
    [DataField]
    public float NutritionCost = 1f;

    [DataField]
    public float WaterCost = 1f;

    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Increases plant yield at the cost of nutrients and water.\n" +
               "Overdoses may cause hair to grow inside the body.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.05f * (float) ActualPotency))
            return;

        if (!args.EntityManager.TryGetComponent<HumanoidAppearanceComponent>(args.TargetEntity, out var humanoid))
            return;

        var markingManager = IoCManager.Resolve<MarkingManager>();
        var appearance = args.EntityManager.System<SharedHumanoidAppearanceSystem>();
        var changed = false;

        var hairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, humanoid.Species).Keys.ToList();
        if (hairStyles.Count > 0)
        {
            humanoid.MarkingSet.RemoveCategory(MarkingCategories.Hair);
            appearance.AddMarking(args.TargetEntity, random.Pick(hairStyles), forced: true, humanoid: humanoid);
            changed = true;
        }

        if (humanoid.Sex != Sex.Female)
        {
            var facialHairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, humanoid.Species).Keys.ToList();
            if (facialHairStyles.Count > 0)
            {
                humanoid.MarkingSet.RemoveCategory(MarkingCategories.FacialHair);
                appearance.AddMarking(args.TargetEntity, random.Pick(facialHairStyles), forced: true, humanoid: humanoid);
                changed = true;
            }
        }

        if (!changed)
            return;

        var popup = args.EntityManager.System<SharedPopupSystem>();
        popup.PopupEntity(Loc.GetString("chem-trichogenic-hair-change"), args.TargetEntity);
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        plant.YieldMod += (int) MathF.Round((float) potency);
        plant.NutritionLevel -= NutritionCost;
        plant.WaterLevel -= WaterCost;
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.05f * (float) ActualPotency))
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
