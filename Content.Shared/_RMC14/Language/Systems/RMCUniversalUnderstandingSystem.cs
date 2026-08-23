using Content.Shared._RMC14.Language.Components;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Language.Systems;

public sealed class RMCUniversalUnderstandingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCUniversalUnderstandingComponent, CanUnderstandLanguageEvent>(OnCanUnderstand);
    }

    private void OnCanUnderstand(Entity<RMCUniversalUnderstandingComponent> ent, ref CanUnderstandLanguageEvent args)
    {
        if (_timing.CurTime >= ent.Comp.ExpiresAt)
        {
            RemCompDeferred<RMCUniversalUnderstandingComponent>(ent.Owner);
            return;
        }

        args.CanUnderstand = true;
    }

    public void GrantFor(EntityUid uid, TimeSpan duration)
    {
        var comp = EnsureComp<RMCUniversalUnderstandingComponent>(uid);
        comp.ExpiresAt = _timing.CurTime + duration;
    }
}
