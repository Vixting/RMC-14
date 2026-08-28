using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

/// <summary>
///     cm13 mob_defines.dm DISABILITY_DEAF: persistent deafness, independent of RMC's temporary
///     deafness status effect. See <see cref="RMCDeafDisabilitySystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RMCDeafDisabilitySystem))]
public sealed partial class RMCDeafDisabilityComponent : Component;
