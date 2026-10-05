using Robust.Shared.Audio;
using Robust.Shared.Utility;

// ReSharper disable CheckNamespace
namespace Content.Shared.Explosion;

public sealed partial class ExplosionPrototype
{
    /// <summary>
    ///     cm13 explosion power per point of intensity. When above 0 the tile overlay is drawn as cm13's passing smoke
    ///     wave and a shockwave distortion is shown for strong enough explosions.
    /// </summary>
    [DataField]
    public float PowerPerIntensity;

    [DataField]
    public ResPath? CoreTexturePath;

    [DataField]
    public SoundSpecifier? BigSound;

    [DataField]
    public ResPath? SparkTexturePath;

    [DataField]
    public List<string> SparkStates = new();
}
