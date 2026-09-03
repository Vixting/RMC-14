using Content.Shared._RMC14.Botany;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Pest population growth, carnivorous consumption, & pest tolerance damage
/// </summary>
public sealed class RMCPlantPestSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public const float HydroponicsSpeedMultiplier = 1f;

    /// <summary>
    /// Small chance for the pest population to increase. Runs even on an empty/dead tray tick
    /// </summary>
    public void TickSpawn(RMCPlantTrayComponent tray)
    {
        if (!_random.Prob(0.01f))
            return;

        tray.PestLevel += 0.5f * HydroponicsSpeedMultiplier;
        if (tray.DrawWarnings)
            tray.UpdateSpriteAfterUpdate = true;
    }

    public void Tick(Entity<RMCPlantComponent> plant, RMCPlantTrayComponent tray, RMCPlantTraitsComponent traits)
    {
        if (tray.PestLevel <= 0)
            return;

        if (traits.Carnivorous > 0)
        {
            tray.PestLevel -= HydroponicsSpeedMultiplier;
            plant.Comp.Health += HydroponicsSpeedMultiplier;
        }
        else if (tray.PestLevel > traits.PestTolerance)
        {
            plant.Comp.Health -= HydroponicsSpeedMultiplier;
        }

        if (tray.DrawWarnings)
            tray.UpdateSpriteAfterUpdate = true;
    }
}
