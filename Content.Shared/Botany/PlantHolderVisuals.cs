using Robust.Shared.Serialization;

namespace Content.Shared.Botany
{
    [Serializable, NetSerializable]
    public enum PlantHolderVisuals
    {
        PlantRsi,
        PlantState,
        FlowerRsi,
        FlowerState,
        FlowerColor,
        HealthLight,
        WaterLight,
        NutritionLight,
        AlertLight,
        HarvestLight,
    }
}
