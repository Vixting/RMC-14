using Content.Shared.Speech.Muting;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

public sealed class RMCMuteDisabilitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCMuteDisabilityComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCMuteDisabilityComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<RMCMuteDisabilityComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<MutedComponent>(ent.Owner);
    }

    private void OnShutdown(Entity<RMCMuteDisabilityComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<MutedComponent>(ent.Owner);
    }
}
