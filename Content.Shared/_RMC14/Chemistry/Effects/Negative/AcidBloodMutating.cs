using System.Linq;
using Content.Shared.Botany;
using Content.Shared.Botany.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class AcidBloodMutating : RMCChemicalEffect
{
    [DataField]
    public bool Royal;

    [DataField]
    public ProtoId<ReagentPrototype> SelfChem = "RMCXenoBlood";

    [DataField]
    public float MutateChance = 0.1f;

    [DataField]
    public int MaxChemicals = 10;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Corrodes plants in a hydroponics tray and scrambles the chemicals they produce.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (plant.Seed is not { } seed)
            return;

        // cm toxins += 3*volume (royal 6), health -= volume (royal 4*volume)
        var p = (float) potency;
        plant.Toxins += (Royal ? 6f : 3f) * p;
        plant.Health -= (Royal ? 4f : 1f) * p;

        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(MutateChance))
            return;

        if (!seed.Unique)
            plant.Seed = seed = seed.Clone();

        if (Royal)
        {
            if (seed.Chemicals.Count > MaxChemicals)
                return;

            var prototype = IoCManager.Resolve<IPrototypeManager>();
            var hydro = prototype.EnumeratePrototypes<ReagentPrototype>()
                .Where(r => !r.Abstract && r.ChemClass == ChemClass.Hydro)
                .Select(r => r.ID)
                .ToList();

            if (hydro.Count == 0)
                return;

            var pick = random.Pick(hydro);
            seed.Chemicals.TryAdd(pick, new SeedChemQuantity { Min = 1, Max = random.Next(2, 4), PotencyDivisor = 20, Inherent = false });
        }
        else if (seed.Chemicals.Count > 1)
        {
            var removed = random.Pick(seed.Chemicals.Keys.ToList());
            seed.Chemicals.Remove(removed);
        }
        else
        {
            seed.Chemicals.TryAdd(SelfChem, new SeedChemQuantity { Min = 1, Max = 2, PotencyDivisor = 20, Inherent = false });
        }
    }
}
