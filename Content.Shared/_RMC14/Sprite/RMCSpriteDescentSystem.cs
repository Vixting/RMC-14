using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Sprite;

public sealed class RMCSpriteDescentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCSpriteDescentComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RMCSpriteDescentComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.StartTime = _timing.CurTime;
        Dirty(ent);
    }

    public TimeSpan GetDuration(EntProtoId prototype)
    {
        if (_prototypes.Index(prototype).TryGetComponent(out RMCSpriteDescentComponent? descent, EntityManager.ComponentFactory))
            return descent.Duration;

        return TimeSpan.Zero;
    }
}
