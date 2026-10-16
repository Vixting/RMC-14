using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Spawns a particle effect on each projectile fired by gun.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class ParticleOnGunShotProjectileComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<ParticleEffectPrototype> Effect;

    [DataField, AutoNetworkedField]
    public Color? ColorOverride;
}
