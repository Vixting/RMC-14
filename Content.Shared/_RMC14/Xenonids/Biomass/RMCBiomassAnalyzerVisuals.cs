using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Biomass;

[Serializable, NetSerializable]
public enum RMCBiomassAnalyzerVisuals
{
    State,
}

[Serializable, NetSerializable]
public enum RMCBiomassAnalyzerVisualState
{
    Idle,
    Printing,
}

public enum RMCBiomassAnalyzerVisualLayers
{
    Printing,
}
