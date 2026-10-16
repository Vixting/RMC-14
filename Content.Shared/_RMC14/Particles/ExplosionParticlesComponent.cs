namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Particle effects played at an explosions epicenter, picked by epicenter power.
/// </summary>
[RegisterComponent]
[Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class ExplosionParticlesComponent : Component
{
    /// <summary>
    /// Used for anything below the weakest tier.
    /// </summary>
    [DataField(required: true)]
    public ExplosionParticleTier Tiny = new();

    [DataField]
    public List<ExplosionParticleTier> Tiers = new();
}
