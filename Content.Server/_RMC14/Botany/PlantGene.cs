using Content.Server.Botany;
using Content.Shared._RMC14.Botany;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Botany;

public sealed class PlantGene
{
    public PlantGeneType Type;

    public string? DisplayLabel;

    public Dictionary<string, SeedChemQuantity>? Chemicals;
    public HarvestType? HarvestRepeat;

    public float? NutrientConsumption;
    public float? WaterConsumption;

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
    public float? Potency;
    public float? Lifespan;
    public float? Maturation;
    public float? Production;

    public ResPath? PlantRsi;
    public string? PlantIconState;

    public static PlantGene FromSeed(SeedData seed, PlantGeneType type)
    {
        var gene = new PlantGene { Type = type };
        switch (type)
        {
            case PlantGeneType.Products:
                gene.Chemicals = new Dictionary<string, SeedChemQuantity>(seed.Chemicals);
                gene.HarvestRepeat = seed.HarvestRepeat;
                break;
            case PlantGeneType.Consumption:
                gene.NutrientConsumption = seed.NutrientConsumption;
                gene.WaterConsumption = seed.WaterConsumption;
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
                gene.Potency = seed.Potency;
                gene.Lifespan = seed.Lifespan;
                gene.Maturation = seed.Maturation;
                gene.Production = seed.Production;
                break;
            case PlantGeneType.Flowers:
                gene.PlantRsi = seed.PlantRsi;
                gene.PlantIconState = seed.PlantIconState;
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
                    foreach (var chem in Chemicals)
                        seed.Chemicals[chem.Key] = chem.Value;
                    // Penalty for splicing in foreign chemistry (-15% vigour stats).
                    seed.Endurance = MathF.Max(1f, seed.Endurance * 0.85f);
                    seed.Yield = Math.Max(1, (int)(seed.Yield * 0.85f));
                    seed.Lifespan = MathF.Max(1f, seed.Lifespan * 0.85f);
                }
                if (HarvestRepeat.HasValue)
                    seed.HarvestRepeat = HarvestRepeat.Value;
                break;
            case PlantGeneType.Consumption:
                seed.NutrientConsumption = NutrientConsumption!.Value;
                seed.WaterConsumption = WaterConsumption!.Value;
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
                seed.Potency = Potency!.Value;
                seed.Lifespan = Lifespan!.Value;
                seed.Maturation = Maturation!.Value;
                seed.Production = Production!.Value;
                break;
            case PlantGeneType.Flowers:
                seed.PlantRsi = PlantRsi!.Value;
                seed.PlantIconState = PlantIconState!;
                break;
        }
    }

    public string GetLabel()
    {
        return DisplayLabel ?? Loc.GetString($"rmc-gene-type-{Type.ToString().ToLower()}");
    }
}
