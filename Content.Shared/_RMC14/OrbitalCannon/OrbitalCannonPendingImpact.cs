using Robust.Shared.Map;

namespace Content.Shared._RMC14.OrbitalCannon;

public readonly record struct OrbitalCannonPendingImpact(
    TimeSpan At,
    OrbitalCannonExplosion Step,
    MapCoordinates MapCoordinates,
    EntityCoordinates Coordinates
);
