using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

[RegisterComponent]
[Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class ParticleOnVehicleDamageComponent : Component
{
    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> DamagedEffects = new();

    [DataField]
    public float DamagedThreshold = 0.5f;

    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> DestroyedEffects = new();

    [DataField]
    public TimeSpan FlareDuration = TimeSpan.FromSeconds(12);

    [DataField]
    public float WispFraction;
}
