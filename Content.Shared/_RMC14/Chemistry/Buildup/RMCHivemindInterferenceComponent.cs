using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Buildup;

/// <summary>
///     cm13's /datum/component/status_effect/interference: a decaying buildup that blocks a xeno's
///     hivemind chat channel while it dissipates. Applied by the Disrupting chem property on touch.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHivemindInterferenceComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Magnitude;

    /// <summary>
    ///     How much <see cref="Magnitude"/> dissipates per second.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DissipationRate = 1f;

    [DataField, AutoNetworkedField]
    public float MaxBuildup = 90f;
}
