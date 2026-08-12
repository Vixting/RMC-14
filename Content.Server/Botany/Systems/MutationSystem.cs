using System.Linq;
using Content.Shared.Botany;
using Content.Shared.Botany.Components;
using Content.Shared.Atmos;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Botany;

public sealed class MutationSystem : EntitySystem
{
    private static readonly ProtoId<WeightedRandomFillSolutionPrototype> RandomPickBotanyReagent = "RandomPickBotanyReagent";

    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private static readonly Color[] BioluminescentColors =
    [
        Color.FromHex("#FF0000"), Color.FromHex("#FF7F00"), Color.FromHex("#FFFF00"),
        Color.FromHex("#00FF00"), Color.FromHex("#0000FF"), Color.FromHex("#4B0082"),
        Color.FromHex("#8F00FF"),
    ];

    private static class Slot
    {
        public const string PlantCancer = "Plant Cancer";
        public const string Gluttony = "Gluttony";
        public const string Endurance = "Endurance";
        public const string LightTolerance = "Light Tolerance";
        public const string ToxinTolerance = "Toxin Tolerance";
        public const string WeedTolerance = "Weed Tolerance";
        public const string Production = "Production";
        public const string Lifespan = "Lifespan";
        public const string Potency = "Potency";
        public const string Maturity = "Maturity";
        public const string Bioluminescence = "Bioluminescence";
        public const string Flowers = "Flowers";
        public const string NewChems = "New Chems";
        public const string NewChems2 = "New Chems2";
        public const string NewChems3 = "New Chems3";
        public const string MutateSpecies = "Mutate Species";
    }

    private static readonly string[] MutationSlots =
    [
        Slot.PlantCancer,
        Slot.Gluttony,
        Slot.Endurance,
        Slot.LightTolerance,
        Slot.ToxinTolerance,
        Slot.WeedTolerance,
        Slot.Production,
        Slot.Lifespan,
        Slot.Potency,
        Slot.Maturity,
        Slot.Bioluminescence,
        Slot.Flowers,
        Slot.NewChems,
        Slot.NewChems2,
        Slot.NewChems3,
    ];

    /// <summary>
    ///     Applies 1-3 random mutations filtered by MutationController.
    ///     MutationController slots are reset to 0 after each call (unless strongly suppressed at &lt;= -3).
    /// </summary>
    public void MutateSeed(EntityUid plantHolder, ref SeedData seed, float severity)
    {
        if (!seed.Unique)
        {
            Log.Error("Attempted to mutate a shared seed");
            return;
        }

        if (seed.Immutable)
            return;

        var degree = severity <= 8f ? 1 : 2;

        var controller = seed.MutationController;

        var speciesVal = controller.GetValueOrDefault(Slot.MutateSpecies, 0f);
        if (seed.MutationPrototypes.Count > 0 && ((degree > 1 && speciesVal == 0f) || speciesVal > 0f))
        {
            var targetProto = _robustRandom.Pick(seed.MutationPrototypes);
            if (_prototypeManager.TryIndex(targetProto, out SeedPrototype? protoSeed))
                seed = seed.SpeciesChange(protoSeed);

            ResetController(seed);
            return;
        }

        var superAllowed = new List<string>();
        var normalAllowed = new List<string>();
        var cancelSlots = new HashSet<string>();

        foreach (var slotName in MutationSlots)
        {
            var val = controller.GetValueOrDefault(slotName, 0f);

            if (val > 0f)
                superAllowed.Add(slotName);
            else if (val >= -1f)
            {
                normalAllowed.Add(slotName);
                if (val == -1f)
                    cancelSlots.Add(slotName);
            }
            // val < -1 means suppressed — excluded from both lists
        }

        var candidates = superAllowed.Count > 0 ? superAllowed : normalAllowed;

        if (candidates.Count == 0)
        {
            ResetController(seed);
            return;
        }

        var mutationLevel = TryComp(plantHolder, out PlantHolderComponent? holder) ? holder.MutationLevel : 0f;
        var iterations = _robustRandom.Next(1, degree + 2) + (int) MathF.Round(mutationLevel / 50f) + 1;

        for (var i = 0; i < iterations; i++)
        {
            var slot = _robustRandom.Pick(candidates);

            if (cancelSlots.Contains(slot))
                return;

            ApplyMutation(plantHolder, ref seed, slot, degree);
        }

        ResetController(seed);
    }

    private void ResetController(SeedData seed)
    {
        foreach (var key in seed.MutationController.Keys.ToList())
        {
            if (seed.MutationController[key] > -3f)
                seed.MutationController[key] = 0f;
        }
    }

    private void ApplyMutation(EntityUid plantHolder, ref SeedData seed, string slot, int degree)
    {
        switch (slot)
        {
            case Slot.PlantCancer:
                seed.Lifespan = MathF.Max(0f, seed.Lifespan - _robustRandom.Next(1, 6));
                seed.Endurance = MathF.Max(0f, seed.Endurance - _robustRandom.Next(10, 21));
                break;

            case Slot.Gluttony:
                seed.NutrientConsumption = Math.Clamp(
                    seed.NutrientConsumption + _robustRandom.NextFloat(-degree * 0.1f, degree * 0.1f),
                    0f, 5f);
                seed.WaterConsumption = Math.Clamp(
                    seed.WaterConsumption + _robustRandom.NextFloat(-degree, degree),
                    0f, 50f);
                break;

            case Slot.Endurance:
                seed.Endurance = Math.Clamp(
                    seed.Endurance + _robustRandom.Next(-5, 6) * degree,
                    10f, 100f);
                break;

            case Slot.LightTolerance:
                seed.IdealLight = Math.Clamp(
                    seed.IdealLight + _robustRandom.Next(-1, 2) * degree,
                    0f, 30f);
                seed.LightTolerance = Math.Clamp(
                    seed.LightTolerance + _robustRandom.Next(-2, 3) * degree,
                    0f, 10f);
                break;

            case Slot.ToxinTolerance:
                seed.ToxinsTolerance = Math.Clamp(
                    seed.ToxinsTolerance + _robustRandom.Next(-2, 3) * degree,
                    0f, 10f);
                break;

            case Slot.WeedTolerance:
                seed.WeedTolerance = Math.Clamp(
                    seed.WeedTolerance + _robustRandom.Next(-2, 3) * degree,
                    0f, 10f);
                if (_robustRandom.Prob(degree * 0.05f))
                    seed.Carnivorous = Math.Clamp(seed.Carnivorous + _robustRandom.Next(-degree, degree + 1), 0, 2);
                else if (_robustRandom.Prob(degree * 0.05f))
                    seed.Parasite = !seed.Parasite;
                break;

            case Slot.Production:
                seed.Production = Math.Clamp(
                    seed.Production + _robustRandom.Next(-1, 2) * degree,
                    1f, 10f);
                break;

            case Slot.Lifespan:
                seed.Lifespan = Math.Clamp(
                    seed.Lifespan + _robustRandom.Next(-2, 3) * degree,
                    10f, 30f);
                if (seed.Yield != -1)
                    seed.Yield = Math.Clamp(seed.Yield + _robustRandom.Next(-2, 3) * degree, 0, 10);
                break;

            case Slot.Potency:
                seed.Potency = Math.Clamp(
                    seed.Potency + _robustRandom.Next(-20, 21) * degree,
                    0f, 200f);
                break;

            case Slot.Maturity:
                seed.Maturation = Math.Clamp(
                    seed.Maturation + _robustRandom.Next(-1, 2) * degree,
                    0f, 30f);
                if (_robustRandom.Prob(degree * 0.05f))
                    seed.HarvestRepeat = seed.HarvestRepeat == HarvestType.NoRepeat
                        ? HarvestType.Repeat
                        : HarvestType.NoRepeat;
                break;

            case Slot.Bioluminescence:
                if (_robustRandom.Prob(degree * 0.02f))
                {
                    seed.Bioluminescent = !seed.Bioluminescent;
                    if (seed.Bioluminescent && _robustRandom.Prob(degree * 0.02f))
                        seed.BioluminescentColor = _robustRandom.Pick(BioluminescentColors);
                }
                break;

            case Slot.Flowers:
                if (_robustRandom.Prob(degree * 0.02f))
                {
                    seed.Flowers = !seed.Flowers;
                    if (seed.Flowers && _robustRandom.Prob(degree * 0.02f))
                        seed.FlowerColor = _robustRandom.Pick(BioluminescentColors);
                }
                break;

            case Slot.NewChems:
            case Slot.NewChems2:
            case Slot.NewChems3:
                AddRandomChem(ref seed);
                break;
        }
    }

    private void AddRandomChem(ref SeedData seed)
    {
        // 60u chem cap - no mutations after
        if (ChemOutputFull(seed))
            return;

        if (seed.SpecialChemicals.Count > 0 && _robustRandom.Prob(0.4f))
        {
            var specialId = _robustRandom.Pick(seed.SpecialChemicals);
            if (!seed.Chemicals.ContainsKey(specialId))
            {
                seed.Chemicals[specialId] = new SeedChemQuantity
                {
                    Min = 7,
                    Max = 15,
                    PotencyDivisor = _robustRandom.Next(5, 9),
                    Inherent = false,
                };
            }

            return;
        }

        var fills = _prototypeManager.Index(RandomPickBotanyReagent).Fills;
        if (fills.Count == 0)
            return;

        var pick = _robustRandom.Pick(fills);
        if (pick.Reagents.Count == 0)
            return;

        var chemicalId = _robustRandom.Pick(pick.Reagents);
        var amount = _robustRandom.Next(1, (int) pick.Quantity + 1);

        var chemicals = seed.Chemicals;
        SeedChemQuantity seedChemQuantity;
        if (chemicals.TryGetValue(chemicalId, out var existing))
        {
            seedChemQuantity = new SeedChemQuantity { Min = existing.Min, Max = existing.Max + amount, Inherent = existing.Inherent };
        }
        else
        {
            seedChemQuantity = new SeedChemQuantity { Min = 1, Max = 1 + amount, Inherent = false };
        }
        seedChemQuantity.PotencyDivisor = (int) Math.Ceiling(100.0 / seedChemQuantity.Max);
        chemicals[chemicalId] = seedChemQuantity;
    }

    private static bool ChemOutputFull(SeedData seed)
    {
        var total = 0f;
        foreach (var (_, q) in seed.Chemicals)
        {
            var amount = (float) q.Min;
            if (q.PotencyDivisor > 0 && seed.Potency > 0)
                amount += seed.Potency / q.PotencyDivisor;
            total += Math.Clamp(amount, q.Min, q.Max);
        }

        return total >= 60f;
    }

    public SeedData Cross(SeedData a, SeedData b)
    {
        SeedData result = b.Clone();

        CrossChemicals(ref result.Chemicals, a.Chemicals);

        CrossFloat(ref result.NutrientConsumption, a.NutrientConsumption);
        CrossFloat(ref result.WaterConsumption, a.WaterConsumption);
        CrossFloat(ref result.IdealHeat, a.IdealHeat);
        CrossFloat(ref result.HeatTolerance, a.HeatTolerance);
        CrossFloat(ref result.IdealLight, a.IdealLight);
        CrossFloat(ref result.LightTolerance, a.LightTolerance);
        CrossFloat(ref result.ToxinsTolerance, a.ToxinsTolerance);
        CrossFloat(ref result.LowPressureTolerance, a.LowPressureTolerance);
        CrossFloat(ref result.HighPressureTolerance, a.HighPressureTolerance);
        CrossFloat(ref result.PestTolerance, a.PestTolerance);
        CrossFloat(ref result.WeedTolerance, a.WeedTolerance);

        CrossFloat(ref result.Endurance, a.Endurance);
        CrossInt(ref result.Yield, a.Yield);
        CrossFloat(ref result.Lifespan, a.Lifespan);
        CrossFloat(ref result.Maturation, a.Maturation);
        CrossFloat(ref result.Production, a.Production);
        CrossFloat(ref result.Potency, a.Potency);

        CrossBool(ref result.Seedless, a.Seedless);
        CrossBool(ref result.Ligneous, a.Ligneous);
        CrossBool(ref result.TurnIntoKudzu, a.TurnIntoKudzu);
        CrossBool(ref result.CanScream, a.CanScream);
        CrossBool(ref result.Parasite, a.Parasite);
        CrossInt(ref result.Carnivorous, a.Carnivorous);

        CrossGasses(ref result.ExudeGasses, a.ExudeGasses);
        CrossGasses(ref result.ConsumeGasses, a.ConsumeGasses);

        // LINQ Explanation
        // For the list of mutation effects on both plants, use a 50% chance to pick each one.
        // Union all of the chosen mutations into one list, and pick ones with a Distinct (unique) name.
        result.Mutations = result.Mutations.Where(m => Random(0.5f)).Union(a.Mutations.Where(m => Random(0.5f))).DistinctBy(m => m.Name).ToList();

        // Hybrids have a high chance of being seedless. Balances very
        // effective hybrid crossings.
        if (a.Name != result.Name && Random(0.7f))
        {
            result.Seedless = true;
        }

        return result;
    }

    private void CrossChemicals(ref Dictionary<string, SeedChemQuantity> val, Dictionary<string, SeedChemQuantity> other)
    {
        // Go through chemicals from the pollen in swab
        foreach (var otherChem in other)
        {
            // if both have same chemical, randomly pick potency ratio from the two.
            if (val.ContainsKey(otherChem.Key))
            {
                val[otherChem.Key] = Random(0.5f) ? otherChem.Value : val[otherChem.Key];
            }
            // if target plant doesn't have this chemical, has 50% chance to add it.
            else
            {
                if (Random(0.5f))
                {
                    var fixedChem = otherChem.Value;
                    fixedChem.Inherent = false;
                    val.Add(otherChem.Key, fixedChem);
                }
            }
        }

        // if the target plant has chemical that the pollen in swab does not, 50% chance to remove it.
        foreach (var thisChem in val)
        {
            if (!other.ContainsKey(thisChem.Key))
            {
                if (Random(0.5f))
                {
                    if (val.Count > 1)
                    {
                        val.Remove(thisChem.Key);
                    }
                }
            }
        }
    }

    private void CrossGasses(ref Dictionary<Gas, float> val, Dictionary<Gas, float> other)
    {
        // Go through gasses from the pollen in swab
        foreach (var otherGas in other)
        {
            // if both have same gas, randomly pick ammount from the two.
            if (val.ContainsKey(otherGas.Key))
            {
                val[otherGas.Key] = Random(0.5f) ? otherGas.Value : val[otherGas.Key];
            }
            // if target plant doesn't have this gas, has 50% chance to add it.
            else
            {
                if (Random(0.5f))
                {
                    val.Add(otherGas.Key, otherGas.Value);
                }
            }
        }
        // if the target plant has gas that the pollen in swab does not, 50% chance to remove it.
        foreach (var thisGas in val)
        {
            if (!other.ContainsKey(thisGas.Key))
            {
                if (Random(0.5f))
                {
                    val.Remove(thisGas.Key);
                }
            }
        }
    }

    private void CrossFloat(ref float val, float other)
    {
        val = Random(0.5f) ? val : other;
    }

    private void CrossInt(ref int val, int other)
    {
        val = Random(0.5f) ? val : other;
    }

    private void CrossBool(ref bool val, bool other)
    {
        val = Random(0.5f) ? val : other;
    }

    private bool Random(float p)
    {
        return _robustRandom.Prob(p);
    }
}
