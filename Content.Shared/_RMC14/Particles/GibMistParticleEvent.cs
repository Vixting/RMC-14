using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Particles;

[Serializable, NetSerializable]
public sealed class GibMistParticleEvent : EntityEventArgs
{
    public MapCoordinates Coords;
    public Color BloodColor;

    public GibMistParticleEvent(MapCoordinates coords, Color bloodColor)
    {
        Coords = coords;
        BloodColor = bloodColor;
    }
}
