using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

/// <summary>
///     cm13 mob_defines.dm DISABILITY_BLIND: full, persistent blindness. Distinct from the
///     Traits system's <see cref="Content.Shared.Traits.Assorted.PermanentBlindnessComponent"/>,
///     which only applies its effect on <c>MapInitEvent</c> (character-creation time) and can't be
///     safely granted mid-round. See <see cref="RMCVisionDisabilitySystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RMCVisionDisabilitySystem))]
public sealed partial class RMCBlindDisabilityComponent : Component;
