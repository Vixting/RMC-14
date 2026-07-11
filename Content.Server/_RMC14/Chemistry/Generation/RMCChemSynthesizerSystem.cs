using Content.Server._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCChemSynthesizerSystem : SharedRMCChemSynthesizerSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCChemicalGeneratorSystem _generator = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    protected override void DoSynthesis(Entity<RMCChemSynthesizerComponent> ent)
    {
        var comp = ent.Comp;
        comp.Processing = false;
        Dirty(ent);

        var reagent = _generator.GenerateReagent(comp.Tier);

        var xform = Transform(ent);
        var bottle = Spawn(comp.OutputBottle, xform.Coordinates);
        if (_solution.TryGetSolution(bottle, "drink", out var soln, out _))
            _solution.TryAddReagent(soln.Value, reagent, comp.OutputAmount);

        _audio.PlayPvs(comp.FinishSound, ent);
    }
}
