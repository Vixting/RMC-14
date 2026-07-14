using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCReagentAnalyzerSystem : SharedRMCReagentAnalyzerSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCChemicalGeneratorSystem _generator = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly RMCChemistryResearchSystem _research = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    protected override void DoAnalysis(Entity<RMCReagentAnalyzerComponent> ent)
    {
        var comp = ent.Comp;
        comp.Processing = false;
        Dirty(ent);

        try
        {
            AnalyzeSample(ent);
        }
        catch (Exception e)
        {
            Log.Error($"Error analyzing reagent sample on {ToPrettyString(ent)}: {e}");
            Fail(ent, "rmc-reagent-analyzer-empty");
        }
    }

    private void AnalyzeSample(Entity<RMCReagentAnalyzerComponent> ent)
    {
        var comp = ent.Comp;

        var sample = GetSample(ent);
        if (sample is not { } sampleUid ||
            !_solution.TryGetMixableSolution(sampleUid, out _, out var solution) ||
            solution.Contents.Count == 0)
        {
            Fail(ent, "rmc-reagent-analyzer-empty");
            return;
        }

        if (solution.Contents.Count > 1)
        {
            Fail(ent, "rmc-reagent-analyzer-contaminated");
            return;
        }

        if (solution.Volume < comp.RequiredVolume)
        {
            Fail(ent, "rmc-reagent-analyzer-insufficient", ("required", comp.RequiredVolume));
            return;
        }

        var reagentId = solution.Contents[0].Reagent.Prototype;
        if (!_rmcReagent.TryIndex(reagentId, out var reagent))
        {
            Fail(ent, "rmc-reagent-analyzer-empty");
            return;
        }

        _metaData.SetEntityName(sampleUid, Loc.GetString("rmc-reagent-analyzer-identified-container", ("name", reagent.LocalizedName)));

        comp.VisualState = RMCReagentAnalyzerVisualState.Finished;
        Dirty(ent);
        UpdateAppearance(ent);

        _generator.PrintReport(Transform(ent).Coordinates, reagentId, reagent, comp.SampleNumber++, "XRF Scans");
        _audio.PlayPvs(comp.FinishSound, ent);

        if (reagent.ChemClass >= ChemClass.Special)
        {
            var tier = _generator.TryGetGeneratedTier(reagentId, out var t) ? t : 1;
            _research.TryAwardIdentificationCredits(reagentId, tier);
        }
    }

    private void Fail(Entity<RMCReagentAnalyzerComponent> ent, string locId, params (string, object)[] args)
    {
        var comp = ent.Comp;
        comp.VisualState = RMCReagentAnalyzerVisualState.Failed;
        Dirty(ent);
        UpdateAppearance(ent);

        _generator.PrintErrorReport(Transform(ent).Coordinates, locId, comp.SampleNumber++, args);
        _audio.PlayPvs(comp.FinishSound, ent);
    }
}
