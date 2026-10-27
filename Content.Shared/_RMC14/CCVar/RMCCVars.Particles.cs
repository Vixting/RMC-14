using Robust.Shared.Configuration;

namespace Content.Shared._RMC14.CCVar;

public sealed partial class RMCCVars
{
    /// <summary>
    /// Particle effect quality. 0=Off, 1=Low (25%), 2=Medium (50%), 3=High (100%).
    /// IgnoreQualitySettings particles always render at full quality.
    /// </summary>
    public static readonly CVarDef<int> RMCParticleQuality =
        CVarDef.Create("rmc.particles_quality", 3, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum live particles across all emitters. Overridden by ParticleQuality presets.
    /// </summary>
    public static readonly CVarDef<int> RMCParticleGlobalBudget =
        CVarDef.Create("rmc.particles_global_budget", 8000, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Max IgnoreQualitySettings emitters allowed while ParticleQuality is off.
    /// </summary>
    public static readonly CVarDef<int> RMCParticleIgnoreQualityMaxEmitters =
        CVarDef.Create("rmc.particles_ignore_quality_max_emitters", 8, CVar.CLIENTONLY | CVar.ARCHIVE);
}
