using System;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Fluids;

[Serializable, NetSerializable]
public enum RMCFirefighterNozzleVisuals : byte
{
    Mode,
    Layer,
}
