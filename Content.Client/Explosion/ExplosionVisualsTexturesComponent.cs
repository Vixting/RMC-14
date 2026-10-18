using Content.Shared.Explosion;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;

namespace Content.Client.Explosion;

[RegisterComponent]
public sealed partial class ExplosionVisualsTexturesComponent : Component
{
    /// <summary>
    ///     Uid of the client-side point light entity for this explosion.
    /// </summary>
    public EntityUid LightEntity;

    /// <summary>
    ///     How intense an explosion needs to be at a given tile in order to progress to the next fire-intensity RSI state. See also <see cref="FireFrames"/>
    /// </summary>
    public float IntensityPerState;

    /// <summary>
    ///     The textures used for the explosion fire effect. Each fire-state is associated with an explosion
    ///     intensity range, and each stat itself has several textures.
    /// </summary>
    public List<Texture[]> FireFrames = new();

    public Color? FireColor;

    // RMC14
    public ExplosionPrototype? RMCType;

    public float RMCPowerPerIntensity;

    public TimeSpan RMCStartTime;

    public List<Texture> RMCSmokeFrames = new();

    public List<Texture> RMCCoreFrames = new();

    public List<Texture[]> RMCSparkFrames = new();

    public Color RMCHeatColor = Color.White;

    public int RMCSeed;
    // RMC14
}
