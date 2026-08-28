using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

[RegisterComponent, NetworkedComponent, Access(typeof(RMCMuteDisabilitySystem))]
public sealed partial class RMCMuteDisabilityComponent : Component;
