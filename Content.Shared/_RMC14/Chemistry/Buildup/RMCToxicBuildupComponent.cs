using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Buildup;

/// <summary>
///     cm13's /datum/component/status_effect/toxic_buildup: a decaying buildup that temporarily reduces
///     a xeno's effective armor while it dissipates. Applied by the Corrosive chem property on touch.
///     cm13's component also has an <c>ishuman(parent)</c> branch that deals TOX damage as it dissipates,
///     but the Corrosive property only ever calls AddComponent for xenos (<c>isxeno(M)</c>), so that
///     branch is not modeled here.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCToxicBuildupComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Magnitude;

    /// <summary>
    ///     How much <see cref="Magnitude"/> dissipates per second.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DissipationRate = 1f / 3f;

    [DataField, AutoNetworkedField]
    public float MaxBuildup = 75f;
}
