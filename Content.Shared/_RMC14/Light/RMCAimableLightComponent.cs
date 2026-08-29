using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Light;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCAimableLightSystem))]
public sealed partial class RMCAimableLightComponent : Component;
