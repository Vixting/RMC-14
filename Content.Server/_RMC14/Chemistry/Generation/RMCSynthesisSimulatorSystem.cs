using System.Linq;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCSynthesisSimulatorSystem : SharedRMCSynthesisSimulatorSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCChemicalGeneratorSystem _generator = default!;
    [Dependency] private readonly RMCChemistryResearchSystem _research = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;

    protected override void DoSimulation(Entity<RMCSynthesisSimulatorComponent> ent)
    {
        var comp = ent.Comp;
        comp.Simulating = false;

        if (!TryGetSlotReport(ent, comp.TargetSlotId, out _, out var targetReport))
        {
            Dirty(ent);
            UpdateAppearance(ent);
            return;
        }

        var properties = GetReportProperties(targetReport);

        ChemGeneratorPropertyPrototype? referenceProperty = null;
        var referenceLevel = 0;
        if (comp.Mode is SynthesisMode.Relate or SynthesisMode.Add &&
            TryGetSlotReport(ent, comp.ReferenceSlotId, out _, out var referenceReport))
        {
            TryGetProperty(referenceReport, comp.ReferenceProperty, out referenceProperty!, out referenceLevel);
        }

        switch (comp.Mode)
        {
            case SynthesisMode.Amplify:
                if (TryGetProperty(targetReport, comp.TargetProperty, out var amplifyProperty, out var amplifyLevel))
                {
                    properties.RemoveAll(p => p.Property.ID == amplifyProperty.ID);
                    properties.Add((amplifyProperty, amplifyLevel + 1));
                }
                break;

            case SynthesisMode.Suppress:
                if (TryGetProperty(targetReport, comp.TargetProperty, out var suppressProperty, out var suppressLevel))
                {
                    properties.RemoveAll(p => p.Property.ID == suppressProperty.ID);
                    var newLevel = suppressLevel - 1;
                    if (newLevel > 0)
                        properties.Add((suppressProperty, newLevel));
                }
                break;

            case SynthesisMode.Relate:
                if (referenceProperty != null && TryGetProperty(targetReport, comp.TargetProperty, out var relateTarget, out _))
                {
                    properties.RemoveAll(p => p.Property.ID == relateTarget.ID);
                    _generator.InsertProperty(properties, referenceProperty, referenceLevel);
                }
                break;

            case SynthesisMode.Add:
                if (referenceProperty != null)
                    _generator.InsertProperty(properties, referenceProperty, referenceLevel);
                break;
        }

        var overdose = targetReport.Overdose;
        if (comp.Mode != SynthesisMode.Add)
            overdose = overdose <= 5 ? Math.Max(overdose - 1, 1) : Math.Max(overdose - 5, 5);

        var tier = _generator.TryGetGeneratedTier(targetReport.ReagentId, out var t) ? t : 1;

        var candidates = new List<List<RecipeCandidateIngredient>>();
        var newIngredientIds = new List<string?>();
        for (var i = 0; i < 3; i++)
        {
            var (ingredients, newIngredientId) = _generator.RollRecipeCandidate(targetReport.ReagentId, tier, targetReport.ReagentId.Id);
            candidates.Add(ingredients.Select(c => new RecipeCandidateIngredient { Id = c.Id, Amount = c.Amount, Catalyst = c.Catalyst }).ToList());
            newIngredientIds.Add(newIngredientId);
        }

        comp.PendingProperties = properties.Select(p => new ChemReportProperty { PropertyId = p.Property.ID, Level = p.Level }).ToList();
        comp.PendingOverdose = overdose;
        comp.PendingTier = tier;
        comp.RecipeCandidates = candidates;
        comp.PendingNewIngredientIds = newIngredientIds;
        comp.Picking = true;
        Dirty(ent);
        UpdateAppearance(ent);

        _audio.PlayPvs(comp.FinishSound, ent);
    }

    protected override void FinalizeSimulation(Entity<RMCSynthesisSimulatorComponent> ent, int recipeIndex)
    {
        var comp = ent.Comp;
        comp.Picking = false;

        if (!TryGetSlotReport(ent, comp.TargetSlotId, out _, out var targetReport) ||
            comp.PendingProperties is not { } pendingProperties ||
            comp.RecipeCandidates is not { } candidates ||
            recipeIndex < 0 ||
            recipeIndex >= candidates.Count)
        {
            comp.RecipeCandidates = null;
            comp.PendingProperties = null;
            Dirty(ent);
            UpdateAppearance(ent);
            return;
        }

        var properties = new List<(ChemGeneratorPropertyPrototype Property, int Level)>();
        foreach (var p in pendingProperties)
        {
            if (Prototype.TryIndex<ChemGeneratorPropertyPrototype>(p.PropertyId, out var proto))
                properties.Add((proto, p.Level));
        }

        var ingredients = candidates[recipeIndex].Select(c => (c.Id, c.Amount, c.Catalyst)).ToList();

        var baseReagent = _rmcReagent.Index(targetReport.ReagentId);
        var variantId = _generator.GenerateVariant(baseReagent, properties, comp.PendingTier, ingredients, comp.PendingOverdose);

        var cost = GetCost(ent);
        _research.TrySpendCredits(cost);

        if (comp.PendingNewIngredientIds is { } newIngredientIds &&
            recipeIndex < newIngredientIds.Count &&
            newIngredientIds[recipeIndex] is { } newIngredientId &&
            _generator.TryGetChemClass(newIngredientId, out var newIngredientClass) &&
            newIngredientClass >= ChemClass.Rare)
        {
            _research.AddCredits(1);
        }

        if (comp.Mode == SynthesisMode.Add && TryGetSlotReport(ent, comp.ReferenceSlotId, out _, out var referenceReport))
            _research.LockLineage(referenceReport.ReagentId.Id);

        var variantReagent = _rmcReagent.Index(variantId);
        _generator.PrintReport(Transform(ent).Coordinates, variantId, variantReagent, category: "Synthesis Simulations", simulatorReport: true);

        comp.RecipeCandidates = null;
        comp.PendingProperties = null;
        comp.PendingNewIngredientIds = null;
        Dirty(ent);
        UpdateAppearance(ent);

        _audio.PlayPvs(comp.FinishSound, ent);
    }
}
