using Content.Shared._RMC14.Light;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Lights;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PointLightRotationSystem), typeof(RMCAimableLightSystem))]
public sealed partial class PointLightRotationComponent : Component
{
    [DataField, AutoNetworkedField]
    public Angle Rotation { get; set; }
}
