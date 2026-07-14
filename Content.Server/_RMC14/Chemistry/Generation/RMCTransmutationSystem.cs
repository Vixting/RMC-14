using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Chemistry.Generation;

/// <summary>
/// when a solution holds both a procedurally generated variant reagent &
/// its root (or a sibling variant of the same root), the non root loses - its deleted & its
/// volume, scaled down based on how far apart their overdose thresholds are, is added to the
/// survivor. Root always wins if present otherwise whichever reagent appears first in Contents
/// wins (no recency rule, despite some player folklore (folk lol) to the contrary).
/// </summary>
public sealed class RMCTransmutationSystem : EntitySystem
{
    [Dependency] private readonly RMCChemicalGeneratorSystem _generator = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionChanged(Entity<SolutionComponent> ent, ref SolutionChangedEvent args)
    {
        var contents = ent.Comp.Solution.Contents;
        if (contents.Count < 2)
            return;

        var roots = new Dictionary<string, List<ReagentQuantity>>();
        foreach (var quantity in contents)
        {
            var id = quantity.Reagent.Prototype;
            var root = _generator.TryGetOriginalId(id, out var originalId) ? originalId : id;
            roots.GetOrNew(root).Add(quantity);
        }

        foreach (var (root, group) in roots)
        {
            if (group.Count < 2)
                continue;

            var survivor = group[0];
            foreach (var candidate in group)
            {
                if (candidate.Reagent.Prototype == root)
                {
                    survivor = candidate;
                    break;
                }
            }

            foreach (var loser in group)
            {
                if (loser.Reagent.Prototype == survivor.Reagent.Prototype)
                    continue;

                MergeInto(ent, survivor.Reagent.Prototype, loser);
            }

            return;
        }
    }

    private void MergeInto(Entity<SolutionComponent> ent, string survivorId, ReagentQuantity loser)
    {
        if (!_rmcReagent.TryIndex(survivorId, out var survivor) ||
            !_rmcReagent.TryIndex(loser.Reagent.Prototype, out var loserReagent))
        {
            return;
        }

        var survivorOverdose = (float) (survivor.Overdose ?? FixedPoint2.Zero);
        var loserOverdose = (float) (loserReagent.Overdose ?? FixedPoint2.Zero);

        var factor = Math.Clamp(MathF.Max(MathF.Abs(survivorOverdose - loserOverdose), 5f) / 5f, 1f, 3f);
        if (survivorOverdose < 10f)
            factor = 1f;

        var gained = FixedPoint2.New(MathF.Floor((float) loser.Quantity / factor));
        if (gained <= FixedPoint2.Zero)
            return;

        _solutionContainer.RemoveReagent(ent, loser);
        _solutionContainer.TryAddReagent(ent, survivorId, gained);
    }
}
