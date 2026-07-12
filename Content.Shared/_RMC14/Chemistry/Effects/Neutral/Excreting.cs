using Content.Shared._RMC14.Body;
using Content.Shared.Botany.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Excreting : RMCChemicalEffect
{
    [DataField]
    public float ToxinsAmount = 1f;

    [DataField]
    public float WeedsAmount = 0.5f;

    [DataField]
    public float CounterIncrement = 5f;

    [DataField]
    public int MaxPotencyBonus = 5;

    [DataField]
    public float NutrientConsumptionIncrease = 0.25f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes the plant to secrete excess growth hormones, gradually increasing potency.\n" +
               "On mobs, purges other chemicals from the bloodstream.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        if (!bloodstream.TryGetChemicalSolution(args.TargetEntity, out var solutionEnt, out _))
            return;

        var solutionContainer = args.EntityManager.System<SharedSolutionContainerSystem>();
        solutionContainer.RemoveEachReagent(solutionEnt, Potency);
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        plant.Toxins += ToxinsAmount;
        plant.WeedLevel += WeedsAmount;
        plant.PotencyCounter += CounterIncrement;

        if (plant.PotencyCounter < 100f || plant.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.5f))
            return;

        if (!plant.Seed.Unique)
            plant.Seed = plant.Seed.Clone();

        plant.Seed.Potency += random.Next(1, MaxPotencyBonus + 1);
        plant.Seed.NutrientConsumption += NutrientConsumptionIncrease;
        plant.PotencyCounter = 0f;
        var popup = args.EntityManager.System<SharedPopupSystem>();
        popup.PopupEntity(Loc.GetString("plant-excreting-potency-boost"), args.TargetEntity);
    }
}
