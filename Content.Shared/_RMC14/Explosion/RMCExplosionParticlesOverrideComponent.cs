using Content.Shared._RMC14.Particles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class RMCExplosionParticlesOverrideComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId<ExplosionParticlesComponent> Particles;
}
