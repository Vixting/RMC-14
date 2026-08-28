using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

/// <summary>
///     cm13 handle_disabilities(): NERVOUS mobs have a prob(10) chance to stutter roughly every
///     SSmobs tick (2 seconds).
/// </summary>
public sealed class RMCNervousSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(2);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCNervousComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time < comp.NextCheck)
                continue;

            comp.NextCheck = time + CheckInterval;

            if (_random.Prob(0.1f))
                _stuttering.DoStutter(uid, TimeSpan.FromSeconds(2), true);
        }
    }
}
