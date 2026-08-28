using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Buildup;

/// <summary>
///     cm13's /datum/component/status_effect/speed_modifier: a decaying buildup that either slows or
///     speeds up its holder while it dissipates, applying a stamina cost (or heal) to humans proportional
///     to the dissipation rate. Used by the Musclestimulating (buff) and Neurocryogenic (debuff) chem
///     properties on touch.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSpeedBuildupComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Magnitude;

    /// <summary>
    ///     How much <see cref="Magnitude"/> dissipates per second.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DissipationRate = 0.4f;

    [DataField, AutoNetworkedField]
    public float MaxBuildup = 10f;

    /// <summary>
    ///     True for a speed buff (heals stamina as it dissipates), false for a slow (damages stamina).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IncreaseSpeed;
}
