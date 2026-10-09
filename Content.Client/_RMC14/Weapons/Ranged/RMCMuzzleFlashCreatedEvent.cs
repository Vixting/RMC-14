namespace Content.Client._RMC14.Weapons.Ranged;

/// <summary>
/// Raised on a gun after its muzzle flash effect has been spawned & positioned on the client.
/// </summary>
[ByRefEvent]
public readonly record struct RMCMuzzleFlashCreatedEvent(EntityUid Flash, Angle Angle);
