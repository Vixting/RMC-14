using Content.Shared._RMC14.Vehicle;

namespace Content.Client._RMC14.Vehicle;

public sealed class HardpointIntegrityStateSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HardpointIntegrityComponent, AfterAutoHandleStateEvent>(OnIntegrityState);
    }

    private void OnIntegrityState(Entity<HardpointIntegrityComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var ev = new HardpointIntegrityStateChangedEvent();
        RaiseLocalEvent(ent, ref ev);
    }
}
