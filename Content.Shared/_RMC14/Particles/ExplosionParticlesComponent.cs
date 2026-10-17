namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Particle effects played at an explosions epicenter, picked by epicenter power.
/// </summary>
[RegisterComponent]
[Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class ExplosionParticlesComponent : Component
{
    [DataField(required: true)]
    public List<ExplosionParticleTier> Tiers = new();
}
