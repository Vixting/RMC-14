using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Buildup;

/// <summary>
///     cm13's /datum/component/status_effect/healing_reduction: a decaying buildup that deals flat brute
///     damage to humans while active, and blocks xeno healing by its current magnitude. Used by the
///     Hemorrhaging chem property on touch.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHealingReductionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Magnitude;

    /// <summary>
    ///     How much <see cref="Magnitude"/> dissipates per second. Also the flat brute damage per second
    ///     dealt to humans while this component is active, matching cm13.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DissipationRate = 0.4f;

    [DataField, AutoNetworkedField]
    public float MaxBuildup = 50f;
}
