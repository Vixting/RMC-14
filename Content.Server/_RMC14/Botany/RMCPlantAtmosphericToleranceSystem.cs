using Content.Shared._RMC14.Botany;
using Content.Shared.Atmos;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Pressure/heat tolerance damage, using min/max tolerance windows.
/// </summary>
public sealed class RMCPlantAtmosphericToleranceSystem : EntitySystem
{
    public void Tick(Entity<RMCPlantComponent> plant, RMCPlantTrayComponent tray, RMCPlantAtmosphericComponent atmos, GasMixture environment, float healthMod)
    {
        var pressure = environment.Pressure;
        if (pressure < atmos.MinPressure || pressure > atmos.MaxPressure)
        {
            plant.Comp.Health -= healthMod;
            tray.ImproperPressure = true;
            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;
        }
        else
        {
            tray.ImproperPressure = false;
        }

        var temperature = environment.Temperature;
        if (temperature < atmos.MinHeat || temperature > atmos.MaxHeat)
        {
            plant.Comp.Health -= healthMod;
            tray.ImproperHeat = true;
            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;
        }
        else
        {
            tray.ImproperHeat = false;
        }
    }
}
