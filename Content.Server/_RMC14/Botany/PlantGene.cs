using System.Linq;
using Content.Shared.Atmos;
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

    public float? MinHeat;
    public float? MaxHeat;
    public float? MinPressure;
    public float? MaxPressure;

    public float? ToxinsTolerance;
    public float? PestTolerance;
    public float? WeedTolerance;

    public float? Endurance;
    public int? Yield;
    public float? Lifespan;
    public float? Maturation;
    public float? Production;

    public ResPath? PlantRsi;
    public int? GrowthStages;
    public string? PlantIconState;
    public Color? ProductColor;
    public bool? HasFlowers;
    public string? FlowerIcon;
    public Color? FlowerColor;
    public bool? Bioluminescent;
    public Color? BioluminescentColor;
    public float? BioluminescentRadius;

    public static PlantGene FromSnapshot(List<IComponent> snapshot, PlantGeneType type)
    {
        var gene = new PlantGene { Type = type };

        var harvest = Get<RMCPlantHarvestComponent>(snapshot);
        var chemicals = Get<RMCPlantChemicalsComponent>(snapshot);
        var gas = Get<RMCConsumeExudeGasComponent>(snapshot);
        var atmos = Get<RMCPlantAtmosphericComponent>(snapshot);
        var metabolism = Get<RMCPlantMetabolismComponent>(snapshot);
        var traits = Get<RMCPlantTraitsComponent>(snapshot);
        var growth = Get<RMCPlantGrowthComponent>(snapshot);

        switch (type)
        {
            case PlantGeneType.Products:
                if (harvest != null)
                {
                    gene.ProductPrototypes = new List<string>(harvest.ProductPrototypes);
                    gene.HarvestRepeat = harvest.HarvestRepeat;
                }
                if (chemicals != null)
                {
                    gene.Chemicals = new Dictionary<string, SeedChemQuantity>(chemicals.Chemicals);
                    gene.Potency = chemicals.Potency;
                }
                if (gas != null)
                    gene.ExudeGasses = new Dictionary<Gas, float>(gas.ExudeGasses);
                if (atmos != null)
                    gene.AlterTemperature = atmos.AlterTemperature;
                break;

            case PlantGeneType.Consumption:
                if (gas != null)
                    gene.ConsumeGasses = new Dictionary<Gas, float>(gas.ConsumeGasses);
                if (metabolism != null)
                {
                    gene.NutrientConsumption = metabolism.NutrientConsumption;
                    gene.WaterConsumption = metabolism.WaterConsumption;
                }
                if (traits != null)
                {
                    gene.Carnivorous = traits.Carnivorous;
                    gene.Parasite = traits.Parasite;
                }
                break;

            case PlantGeneType.Environment:
                if (atmos != null)
                {
                    gene.MinHeat = atmos.MinHeat;
                    gene.MaxHeat = atmos.MaxHeat;
                    gene.MinPressure = atmos.MinPressure;
                    gene.MaxPressure = atmos.MaxPressure;
                }
                break;

            case PlantGeneType.Resistance:
                if (traits != null)
                {
                    gene.ToxinsTolerance = traits.ToxinsTolerance;
                    gene.PestTolerance = traits.PestTolerance;
                    gene.WeedTolerance = traits.WeedTolerance;
                }
                break;

            case PlantGeneType.Vigour:
                if (growth != null)
                {
                    gene.Endurance = growth.Endurance;
                    gene.Lifespan = growth.Lifespan;
                    gene.Maturation = growth.Maturation;
                    gene.Production = growth.Production;
                }
                if (harvest != null)
                    gene.Yield = harvest.Yield;
                break;

            case PlantGeneType.Flowers:
                if (traits != null)
                {
                    gene.PlantRsi = traits.PlantRsi;
                    gene.PlantIconState = traits.PlantIconState;
                    gene.HasFlowers = traits.Flowers;
                    gene.FlowerIcon = traits.FlowerIcon;
                    gene.FlowerColor = traits.FlowerColor;
                    gene.Bioluminescent = traits.Bioluminescent;
                    gene.BioluminescentColor = traits.BioluminescentColor;
                    gene.BioluminescentRadius = traits.BioluminescentRadius;
                }
                if (growth != null)
                    gene.GrowthStages = growth.GrowthStages;
                if (chemicals != null)
                    gene.ProductColor = chemicals.ProductColor;
                break;
        }

        return gene;
    }

    public void ApplyTo(List<IComponent> snapshot)
    {
        switch (Type)
        {
            case PlantGeneType.Products:
                if (Chemicals != null)
                {
                    var chemicals = GetOrCreate<RMCPlantChemicalsComponent>(snapshot);
                    foreach (var (reagentId, incoming) in Chemicals)
                    {
                        if (chemicals.Chemicals.TryGetValue(reagentId, out var existing))
                        {
                            chemicals.Chemicals[reagentId] = new SeedChemQuantity
                            {
                                Min = Math.Max(1, (existing.Min + incoming.Min) / 2),
                                Max = Math.Max(1, (existing.Max + incoming.Max) / 2),
                                PotencyDivisor = incoming.PotencyDivisor,
                                Inherent = incoming.Inherent,
                            };
                        }
                        else
                        {
                            chemicals.Chemicals[reagentId] = incoming;
                        }
                    }

                    if (ProductPrototypes != null)
                    {
                        var harvestProducts = GetOrCreate<RMCPlantHarvestComponent>(snapshot);
                        foreach (var product in ProductPrototypes)
                        {
                            if (!harvestProducts.ProductPrototypes.Contains(product))
                                harvestProducts.ProductPrototypes.Add(product);
                        }
                    }

                    if (ExudeGasses != null)
                    {
                        var gas = GetOrCreate<RMCConsumeExudeGasComponent>(snapshot);
                        foreach (var (gasType, amount) in ExudeGasses)
                            gas.ExudeGasses[gasType] = MathF.Max(1f, amount * 0.8f);
                    }

                    if (AlterTemperature is { } alterTemperature)
                        GetOrCreate<RMCPlantAtmosphericComponent>(snapshot).AlterTemperature = alterTemperature;

                    if (Potency is { } potency)
                        chemicals.Potency = potency;

                    // Penalty for splicing in foreign chemistry (-15% vigour stats).
                    var growth = GetOrCreate<RMCPlantGrowthComponent>(snapshot);
                    growth.Endurance = MathF.Max(1f, growth.Endurance * 0.85f);
                    growth.Lifespan = MathF.Max(1f, growth.Lifespan * 0.85f);
                    var harvestYield = GetOrCreate<RMCPlantHarvestComponent>(snapshot);
                    harvestYield.Yield = Math.Max(1, (int)(harvestYield.Yield * 0.85f));
                }
                if (HarvestRepeat.HasValue)
                    GetOrCreate<RMCPlantHarvestComponent>(snapshot).HarvestRepeat = HarvestRepeat.Value;
                break;

            case PlantGeneType.Consumption:
                var metabolism = GetOrCreate<RMCPlantMetabolismComponent>(snapshot);
                if (ConsumeGasses != null)
                    GetOrCreate<RMCConsumeExudeGasComponent>(snapshot).ConsumeGasses = new Dictionary<Gas, float>(ConsumeGasses);
                metabolism.NutrientConsumption = NutrientConsumption!.Value;
                metabolism.WaterConsumption = WaterConsumption!.Value;
                if (Carnivorous is { } carnivorous)
                    GetOrCreate<RMCPlantTraitsComponent>(snapshot).Carnivorous = carnivorous;
                if (Parasite is { } parasite)
                    GetOrCreate<RMCPlantTraitsComponent>(snapshot).Parasite = parasite;
                break;

            case PlantGeneType.Environment:
                var atmos = GetOrCreate<RMCPlantAtmosphericComponent>(snapshot);
                atmos.MinHeat = MinHeat!.Value;
                atmos.MaxHeat = MaxHeat!.Value;
                atmos.MinPressure = MinPressure!.Value;
                atmos.MaxPressure = MaxPressure!.Value;
                break;

            case PlantGeneType.Resistance:
                var traits = GetOrCreate<RMCPlantTraitsComponent>(snapshot);
                traits.ToxinsTolerance = ToxinsTolerance!.Value;
                traits.PestTolerance = PestTolerance!.Value;
                traits.WeedTolerance = WeedTolerance!.Value;
                break;

            case PlantGeneType.Vigour:
                var growthV = GetOrCreate<RMCPlantGrowthComponent>(snapshot);
                growthV.Endurance = Endurance!.Value;
                growthV.Lifespan = Lifespan!.Value;
                growthV.Maturation = Maturation!.Value;
                growthV.Production = Production!.Value;
                GetOrCreate<RMCPlantHarvestComponent>(snapshot).Yield = Yield!.Value;
                break;

            case PlantGeneType.Flowers:
                var traitsF = GetOrCreate<RMCPlantTraitsComponent>(snapshot);
                traitsF.PlantRsi = PlantRsi!.Value;
                traitsF.PlantIconState = PlantIconState!;
                if (HasFlowers is { } hasFlowers)
                    traitsF.Flowers = hasFlowers;
                traitsF.FlowerIcon = FlowerIcon;
                traitsF.FlowerColor = FlowerColor;
                if (Bioluminescent is { } bioluminescent)
                    traitsF.Bioluminescent = bioluminescent;
                if (BioluminescentColor is { } bioluminescentColor)
                    traitsF.BioluminescentColor = bioluminescentColor;
                if (BioluminescentRadius is { } bioluminescentRadius)
                    traitsF.BioluminescentRadius = bioluminescentRadius;
                GetOrCreate<RMCPlantGrowthComponent>(snapshot).GrowthStages = GrowthStages!.Value;
                if (ProductColor != null)
                    GetOrCreate<RMCPlantChemicalsComponent>(snapshot).ProductColor = ProductColor;
                break;
        }
    }

    public string GetLabel()
    {
        return DisplayLabel ?? Type.GetLabel();
    }

    private static T? Get<T>(List<IComponent> snapshot) where T : class, IComponent
    {
        return snapshot.OfType<T>().FirstOrDefault();
    }

    private static T GetOrCreate<T>(List<IComponent> snapshot) where T : class, IComponent, new()
    {
        if (Get<T>(snapshot) is { } existing)
            return existing;

        var created = new T();
        snapshot.Add(created);
        return created;
    }
}
