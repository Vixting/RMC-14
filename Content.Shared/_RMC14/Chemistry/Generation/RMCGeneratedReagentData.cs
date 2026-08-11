using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

[Serializable, NetSerializable]
public sealed record RMCGeneratedReagentData(
    string Id,
    string Name,
    string Color,
    string PhysicalDescription,
    List<ChemReportProperty> Properties,
    int Overdose,
    int CriticalOverdose,
    List<RecipeCandidateIngredient> Ingredients,
    ChemClass ChemClass,
    ReactionIndicator Indicator);
