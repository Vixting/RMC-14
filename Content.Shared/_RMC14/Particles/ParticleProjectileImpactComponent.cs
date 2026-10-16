using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Replaces the default impact, blood & debris particles when this projectile hits something.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ProjectileImpactParticleSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class ParticleProjectileImpactComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<ParticleEffectPrototype>> Effects = new();

    [DataField, AutoNetworkedField]
    public List<ProtoId<ParticleEffectPrototype>> SurfaceEffects = new();

    [DataField, AutoNetworkedField]
    public Color? Color;
}
