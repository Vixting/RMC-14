using Content.Shared._RMC14.Intel;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCIntelVialBoxSystem : EntitySystem
{
    [Dependency] private readonly RMCChemicalGeneratorSystem _generator = default!;
    [Dependency] private readonly IntelSystem _intel = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    private static readonly EntProtoId VialProto = "RMCVial";

    private const int SlotCount = 6;
    private const string VialSolution = "beaker";
    private static readonly FixedPoint2 VialFillAmount = FixedPoint2.New(30);

    public override void Initialize()
    {
        SubscribeLocalEvent<IntelVialBoxComponent, MapInitEvent>(OnVialBoxMapInit);
    }

    private void OnVialBoxMapInit(Entity<IntelVialBoxComponent> box, ref MapInitEvent args)
    {
        var coords = Transform(box).Coordinates;

        // slots are candidates, each with separate 40% chance of holding a real reagent - the rest are always empty
        var spawns = _random.Next(1, 5);

        for (var i = 0; i < SlotCount; i++)
        {
            var vial = Spawn(VialProto, coords);
            _storage.Insert(box.Owner, vial, out _, playSound: false);

            if (i >= spawns)
                continue;

            if (!_random.Prob(0.4f))
                continue;

            var reagentId = _generator.RollRandomKnownReagent();
            if (string.IsNullOrEmpty(reagentId))
                continue;

            if (_solutionContainer.TryGetSolution(vial, VialSolution, out var solution, out _))
                _solutionContainer.TryAddReagent(solution.Value, reagentId, VialFillAmount);

            _intel.FlagRetrieveObjective(vial, FixedPoint2.New(0.1));
        }
    }
}
