using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Spawns particle effect at muzzle each time gun shows muzzle flash.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class ParticleOnMuzzleFlashComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<ParticleEffectPrototype> Effect;

    [DataField, AutoNetworkedField]
    public Color? ColorOverride;

    [DataField, AutoNetworkedField]
    public bool Reverse;
}
