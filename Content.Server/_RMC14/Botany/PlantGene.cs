using Content.Shared.Atmos;
using Content.Shared.Botany;
using Content.Shared._RMC14.Botany;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Botany;

public sealed class PlantGene
{
    public PlantGeneType Type;

    public string? DisplayLabel;

    public List<string>? ProductPrototypes;
    public Dictionary<string, SeedChemQuantity>? Chemicals;
    public Dictionary<Gas, float>? ExudeGasses;
    public float? AlterTemperature;
    public float? Potency;
    public HarvestType? HarvestRepeat;

    public Dictionary<Gas, float>? ConsumeGasses;
    public float? NutrientConsumption;
    public float? WaterConsumption;
    public int? Carnivorous;
    public bool? Parasite;

    public float? IdealHeat;
    public float? HeatTolerance;
    public float? IdealLight;
    public float? LightTolerance;
    public float? LowPressureTolerance;
    public float? HighPressureTolerance;

    public float? ToxinsTolerance;
    public float? PestTolerance;
    public float? WeedTolerance;

    public float? Endurance;
    public int? Yield;
    public float? Lifespan;
    public float? Maturation;
    public float? Production;

    public ResPath? PlantRsi;
    public string? PlantIconState;
    public Color? ProductColor;
    public bool? HasFlowers;
    public string? FlowerIcon;

    public static PlantGene FromSeed(SeedData seed, PlantGeneType type)
    {
        var gene = new PlantGene { Type = type };
        switch (type)
        {
            case PlantGeneType.Products:
                gene.ProductPrototypes = new List<string>(seed.ProductPrototypes);
                gene.Chemicals = new Dictionary<string, SeedChemQuantity>(seed.Chemicals);
                gene.ExudeGasses = new Dictionary<Gas, float>(seed.ExudeGasses);
                gene.AlterTemperature = seed.AlterTemperature;
                gene.Potency = seed.Potency;
                gene.HarvestRepeat = seed.HarvestRepeat;
                break;
            case PlantGeneType.Consumption:
                gene.ConsumeGasses = new Dictionary<Gas, float>(seed.ConsumeGasses);
                gene.NutrientConsumption = seed.NutrientConsumption;
                gene.WaterConsumption = seed.WaterConsumption;
                gene.Carnivorous = seed.Carnivorous;
                gene.Parasite = seed.Parasite;
                break;
            case PlantGeneType.Environment:
                gene.IdealHeat = seed.IdealHeat;
                gene.HeatTolerance = seed.HeatTolerance;
                gene.IdealLight = seed.IdealLight;
                gene.LightTolerance = seed.LightTolerance;
                gene.LowPressureTolerance = seed.LowPressureTolerance;
                gene.HighPressureTolerance = seed.HighPressureTolerance;
                break;
            case PlantGeneType.Resistance:
                gene.ToxinsTolerance = seed.ToxinsTolerance;
                gene.PestTolerance = seed.PestTolerance;
                gene.WeedTolerance = seed.WeedTolerance;
                break;
            case PlantGeneType.Vigour:
                gene.Endurance = seed.Endurance;
                gene.Yield = seed.Yield;
                gene.Lifespan = seed.Lifespan;
                gene.Maturation = seed.Maturation;
                gene.Production = seed.Production;
                break;
            case PlantGeneType.Flowers:
                gene.PlantRsi = seed.PlantRsi;
                gene.PlantIconState = seed.PlantIconState;
                gene.ProductColor = seed.ProductColor;
                gene.HasFlowers = seed.Flowers;
                gene.FlowerIcon = seed.FlowerIcon;
                break;
        }
        return gene;
    }

    public void ApplyTo(SeedData seed)
    {
        switch (Type)
        {
            case PlantGeneType.Products:
                if (Chemicals != null)
                {
                    foreach (var (reagentId, incoming) in Chemicals)
                    {
                        if (seed.Chemicals.TryGetValue(reagentId, out var existing))
                        {
                            seed.Chemicals[reagentId] = new SeedChemQuantity
                            {
                                Min = Math.Max(1, (existing.Min + incoming.Min) / 2),
                                Max = Math.Max(1, (existing.Max + incoming.Max) / 2),
                                PotencyDivisor = incoming.PotencyDivisor,
                                Inherent = incoming.Inherent,
                            };
                        }
                        else
                        {
                            seed.Chemicals[reagentId] = incoming;
                        }
                    }

                    if (ProductPrototypes != null)
                    {
                        foreach (var product in ProductPrototypes)
                        {
                            if (!seed.ProductPrototypes.Contains(product))
                                seed.ProductPrototypes.Add(product);
                        }
                    }

                    if (ExudeGasses != null)
                    {
                        foreach (var (gas, amount) in ExudeGasses)
                            seed.ExudeGasses[gas] = MathF.Max(1f, amount * 0.8f);
                    }

                    if (AlterTemperature is { } alterTemperature)
                        seed.AlterTemperature = alterTemperature;

                    if (Potency is { } potency)
                        seed.Potency = potency;

                    // Penalty for splicing in foreign chemistry (-15% vigour stats).
                    seed.Endurance = MathF.Max(1f, seed.Endurance * 0.85f);
                    seed.Yield = Math.Max(1, (int)(seed.Yield * 0.85f));
                    seed.Lifespan = MathF.Max(1f, seed.Lifespan * 0.85f);
                }
                if (HarvestRepeat.HasValue)
                    seed.HarvestRepeat = HarvestRepeat.Value;
                break;
            case PlantGeneType.Consumption:
                if (ConsumeGasses != null)
                    seed.ConsumeGasses = new Dictionary<Gas, float>(ConsumeGasses);
                seed.NutrientConsumption = NutrientConsumption!.Value;
                seed.WaterConsumption = WaterConsumption!.Value;
                if (Carnivorous is { } carnivorous)
                    seed.Carnivorous = carnivorous;
                if (Parasite is { } parasite)
                    seed.Parasite = parasite;
                break;
            case PlantGeneType.Environment:
                seed.IdealHeat = IdealHeat!.Value;
                seed.HeatTolerance = HeatTolerance!.Value;
                seed.IdealLight = IdealLight!.Value;
                seed.LightTolerance = LightTolerance!.Value;
                seed.LowPressureTolerance = LowPressureTolerance!.Value;
                seed.HighPressureTolerance = HighPressureTolerance!.Value;
                break;
            case PlantGeneType.Resistance:
                seed.ToxinsTolerance = ToxinsTolerance!.Value;
                seed.PestTolerance = PestTolerance!.Value;
                seed.WeedTolerance = WeedTolerance!.Value;
                break;
            case PlantGeneType.Vigour:
                seed.Endurance = Endurance!.Value;
                seed.Yield = Yield!.Value;
                seed.Lifespan = Lifespan!.Value;
                seed.Maturation = Maturation!.Value;
                seed.Production = Production!.Value;
                break;
            case PlantGeneType.Flowers:
                seed.PlantRsi = PlantRsi!.Value;
                seed.PlantIconState = PlantIconState!;
                seed.ProductColor = ProductColor;
                if (HasFlowers is { } hasFlowers)
                    seed.Flowers = hasFlowers;
                seed.FlowerIcon = FlowerIcon;
                break;
        }
    }

    public string GetLabel()
    {
        return DisplayLabel ?? Type.GetLabel();
    }
}
