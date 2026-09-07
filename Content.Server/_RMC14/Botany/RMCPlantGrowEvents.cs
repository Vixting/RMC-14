using Content.Shared.Atmos;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Raised on a tray every growth cycle.  Lets independent tray concerns subscribe instead of being
/// called directly by <see cref="RMCPlantTraySystem"/>.
/// </summary>
[ByRefEvent]
public readonly record struct RMCPlantTrayGrowEvent(EntityUid Tray, EntityUid? Plant);

/// <summary>
/// Raised on a living plant once per growth cycle, after aging and nutrient/water metabolism
/// have run
/// </summary>
[ByRefEvent]
public readonly record struct RMCPlantGrowEvent(EntityUid Plant, EntityUid Tray, GasMixture Environment, float HealthMod);
