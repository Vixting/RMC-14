using Content.Shared.Explosion;
using Robust.Client.Graphics;

// ReSharper disable CheckNamespace
namespace Content.Client.Explosion;

public sealed partial class ExplosionVisualsTexturesComponent
{
    public ExplosionPrototype? RMCType;

    public List<Texture> RMCSmokeFrames = new();

    public List<Texture> RMCCoreFrames = new();

    public List<Texture[]> RMCSparkFrames = new();

    public int RMCSeed;
}
