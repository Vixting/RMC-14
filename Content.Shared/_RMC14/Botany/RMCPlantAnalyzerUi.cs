using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Botany;

[Serializable, NetSerializable]
public enum RMCPlantAnalyzerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public readonly record struct RMCPlantAnalyzerChemical(string ReagentId, string Name, Color Color, int Min, int Max);
