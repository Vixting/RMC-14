using System.Numerics;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Water;

[RegisterComponent]
public sealed partial class RMCWaterOverlayVisualsComponent : Component
{
    public Vector2 OriginalOffset;

    public ShaderInstance? SubmersionShader;

    public int OverlayBucket;
}
