using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class ExplosionProneResistanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Multiplier = 0.5f;
}
