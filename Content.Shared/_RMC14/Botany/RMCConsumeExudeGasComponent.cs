using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

/// <summary>
/// Gas consumption/production for a <see cref="RMCPlantComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class RMCConsumeExudeGasComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<Gas, float> ConsumeGasses = new();

    [DataField, AutoNetworkedField]
    public Dictionary<Gas, float> ExudeGasses = new();
}
