using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class ParticleOnHardpointDamageComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<ParticleEffectPrototype>> DamagedEffects = new();

    [DataField, AutoNetworkedField]
    public float DamagedThreshold = 0.5f;

    [DataField, AutoNetworkedField]
    public bool RequireInstalled = true;

    [DataField, AutoNetworkedField]
    public List<ProtoId<ParticleEffectPrototype>> DestroyedEffects = new();
}
