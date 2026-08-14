using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Water;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWaterOverlaySystem))]
public sealed partial class RMCWaterOverlayComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> Colliding = new();

    [DataField, AutoNetworkedField]
    public WaterDepth EffectiveDepth;

    [DataField, AutoNetworkedField]
    public bool Toxic;

    [ViewVariables]
    public bool InWater => Colliding.Count > 0;

    public float DistanceSinceWadingSound;
}

[ByRefEvent]
public readonly record struct WaterOverlayChangedEvent;
