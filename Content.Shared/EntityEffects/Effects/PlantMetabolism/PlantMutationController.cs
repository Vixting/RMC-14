using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

/// <summary>
///     Port of CM13's *peutic / *toxic property reaction_hydro_tray effects.
///     Sets named mutation controller slots in the plant's seed to enable or suppress specific mutations.
/// </summary>
public sealed partial class PlantMutationController : EventEntityEffect<PlantMutationController>
{
    /// <summary>
    ///     Mutation slot names to modify. Uses CM13 names:
    ///     "Plant Cancer", "Gluttony", "Endurance", "Light Tolerance", "Toxin Tolerance",
    ///     "Weed Tolerance", "Production", "Lifespan", "Potency", "Maturity",
    ///     "Bioluminescence", "Flowers", "New Chems", "New Chems2", "New Chems3", "Mutate Species".
    /// </summary>
    [DataField(required: true)]
    public List<string> Slots { get; private set; } = new();

    /// <summary>
    ///     Whether to enable (true) or suppress (false) the listed slots.
    ///     Enable sets a slot to at least 1 (peutic reagents).
    ///     Suppress sets a slot to at most -2 (toxic reagents).
    /// </summary>
    [DataField]
    public bool Enable { get; private set; } = true;

    /// <summary>The target value when enabling. CM13 peutic always uses 1.</summary>
    [DataField]
    public float EnableValue { get; private set; } = 1f;

    /// <summary>The target value when suppressing. CM13 toxic uses potency*-2 ≈ -1 at default potency.</summary>
    [DataField]
    public float SuppressValue { get; private set; } = -2f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var slotsStr = string.Join(", ", Slots);
        return Enable
            ? Loc.GetString("reagent-effect-guidebook-plant-mutation-enable", ("slots", slotsStr))
            : Loc.GetString("reagent-effect-guidebook-plant-mutation-suppress", ("slots", slotsStr));
    }
}
