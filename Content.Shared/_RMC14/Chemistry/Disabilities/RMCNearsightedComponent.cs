using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

/// <summary>
///     cm13 mob_defines.dm NEARSIGHTED: mild, non-blinding vision blur. Persistent until cured -
///     cm13 requires surgery; RMC-14 currently only clears it via the Aiding/Omnipotent chem
///     properties. See <see cref="RMCVisionDisabilitySystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RMCVisionDisabilitySystem))]
public sealed partial class RMCNearsightedComponent : Component;
