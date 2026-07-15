using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent]
public sealed partial class RMCVolatileFireSpreadComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Spawn;

    [DataField(required: true)]
    public int Range;

    [DataField]
    public int? Intensity;

    [DataField]
    public int? Duration;

    [DataField]
    public bool FirePenetrating;

    [DataField]
    public Color? FireColor;

    [DataField(required: true)]
    public EntityUid Chain;

    [DataField(required: true)]
    public TimeSpan NextSpreadAt;
}
