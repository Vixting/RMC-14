using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Debris thrown when projectile hits entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(StructureDebrisParticleSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class ParticleDebrisComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ParticleEffectPrototype>? Effect;

    [DataField, AutoNetworkedField]
    public int DestructionCount = 24;

    [DataField, AutoNetworkedField]
    public float DestructionIntensity = 1.4f;
}
