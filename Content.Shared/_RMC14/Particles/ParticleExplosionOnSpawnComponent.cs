using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Plays tiny tier of explosion particle set where this entity spawns.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class ParticleExplosionOnSpawnComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId<ExplosionParticlesComponent> Particles;

    [DataField, AutoNetworkedField]
    public float Probability = 1f;
}
