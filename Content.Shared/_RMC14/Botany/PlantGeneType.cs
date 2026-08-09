namespace Content.Shared._RMC14.Botany;

public enum PlantGeneType : byte
{
    Products,
    Consumption,
    Environment,
    Resistance,
    Vigour,
    Flowers,
}

public static class PlantGeneTypeExtensions
{
    public static string GetLabel(this PlantGeneType type)
    {
        return type switch
        {
            PlantGeneType.Products => "Products",
            PlantGeneType.Consumption => "Consumption",
            PlantGeneType.Environment => "Environment",
            PlantGeneType.Resistance => "Resistance",
            PlantGeneType.Vigour => "Vigour",
            PlantGeneType.Flowers => "Flowers",
            _ => type.ToString(),
        };
    }

    public static string GetDescription(this PlantGeneType type)
    {
        return type switch
        {
            PlantGeneType.Products => "chemicals, potency, harvest type",
            PlantGeneType.Consumption => "water, nutrient & gas use",
            PlantGeneType.Environment => "heat, light & pressure",
            PlantGeneType.Resistance => "weed, pest & toxin tolerance",
            PlantGeneType.Vigour => "yield, lifespan & growth rate",
            PlantGeneType.Flowers => "plant appearance",
            _ => string.Empty,
        };
    }
}
