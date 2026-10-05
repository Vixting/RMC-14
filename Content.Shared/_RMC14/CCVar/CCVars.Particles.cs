using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Particle effect quality. 0=Off, 1=Low (25%), 2=Medium (50%), 3=High (100%).
    /// IgnoreQualitySettings particles always render at full quality.
    /// </summary>
    public static readonly CVarDef<int> ParticleQuality =
        CVarDef.Create("particles.quality", 3, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum live particles across all emitters. Overridden by ParticleQuality presets.
    /// </summary>
    public static readonly CVarDef<int> ParticleGlobalBudget =
        CVarDef.Create("particles.global_budget", 8000, CVar.CLIENTONLY | CVar.ARCHIVE);
}
