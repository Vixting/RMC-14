using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

/// <summary>
///     cm13 mob_defines.dm OPIATE_RECEPTOR_DEFICIENCY: reduces Painkilling's effective potency to
///     25%. A pure marker, checked directly wherever it matters - nothing in RMC-14 currently grants
///     it, since cm13 only grants it via a character trait (biology_traits.dm) that RMC-14 doesn't
///     have yet.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCOpiateReceptorDeficiencyComponent : Component;
