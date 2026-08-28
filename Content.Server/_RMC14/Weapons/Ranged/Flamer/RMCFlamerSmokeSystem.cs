using Content.Server.Fluids.EntitySystems;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Weapons.Ranged.Flamer;

/// <summary>
///     Ports cmss13's <c>unleash_smoke()</c> firing mode for the M34/M240 incinerator's smoke tank
///     (<c>/obj/item/ammo_magazine/flamer_tank/smoke</c>) - deploys a chem-smoke cloud carrying the
///     tank's loaded reagent at each tile along the shot line, instead of igniting them.
/// </summary>
/// <remarks>
///     Owns the <see cref="RMCFlamerChainComponent"/> entities <see cref="SharedRMCFlamerSystem"/>
///     creates whenever <c>Smoke</c> is set - that system's own <c>Update</c> explicitly skips them,
///     since spawning smoke needs <see cref="SmokeSystem"/>, which lives in Content.Server and can't
///     be referenced from the shared flamer system.
/// </remarks>
public sealed class RMCFlamerSmokeSystem : EntitySystem
{
    private static readonly FixedPoint2 SmokeUnitsPerTile = FixedPoint2.New(35);
    private const float SmokeDuration = 30f;

    // cm13's unleash_smoke() calls smoke.set_up(to_disperse, 5, loca = turf) at (almost) every tile
    // along the line - set_up's range = n * 0.3, so n=5 is only a ~1.5 tile radius per placement
    // point, tightly bounded. RMC's SpreadAmount instead compounds via EdgeSpreader tile-by-tile
    // expansion, and since a cloud is already placed at every line tile (matching cm13's own per-tile
    // placement), a high SpreadAmount here was multiplying across every one of those placements and
    // covering a much larger area than cm13's actual (small, per-point) spread.
    private const int SmokeSpread = 1;

    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var chains = EntityQueryEnumerator<RMCFlamerChainComponent>();
        while (chains.MoveNext(out var uid, out var comp))
        {
            if (!comp.Smoke)
                continue;

            if (comp.Tiles.Count == 0)
            {
                QueueDel(uid);
                continue;
            }

            foreach (var tile in comp.Tiles)
            {
                if (time < tile.At)
                    continue;

                comp.Tiles.Remove(tile);
                var index = comp.TilesPlaced++;

                // cm13 skips the first tile entirely and shrinks the second to a tiny burst
                // (set_up(to_disperse, 1, ...)) so the shooter doesn't get smoked by their own shot.
                if (index != 0 && _rmcReagent.TryIndex(comp.Reagent, out var reagent))
                {
                    var cloud = Spawn(comp.Spawn, tile.Coordinates);
                    var solution = new Solution(reagent.ID, SmokeUnitsPerTile);
                    var spread = index == 1 ? 0 : SmokeSpread;
                    _smoke.StartSmoke(cloud, solution, SmokeDuration, spread);
                }

                break;
            }
        }
    }
}
