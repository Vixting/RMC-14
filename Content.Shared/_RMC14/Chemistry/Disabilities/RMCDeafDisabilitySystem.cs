using Content.Shared._RMC14.Deafness;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

/// <summary>
///     Grants the same <see cref="DeafComponent"/> RMC's temporary deafness status effect (see
///     <see cref="SharedDeafnessSystem"/>) uses, but directly via component lifecycle rather than
///     the timed-status-effect bookkeeping, so it doesn't expire on its own - mirrors
///     <c>RMCSizeStunSystem</c>'s existing precedent for permanent-while-active deafness.
///     Known limitation: since both share the same underlying component, a concurrent temporary
///     deafness (e.g. a flashbang) whose own timer expires first will remove this too - true
///     independence would need a second, separate "is deaf" component and every hearing check
///     updated to look for both, which isn't worth it for this edge case.
/// </summary>
public sealed class RMCDeafDisabilitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCDeafDisabilityComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCDeafDisabilityComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<RMCDeafDisabilityComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<DeafComponent>(ent.Owner);
    }

    private void OnShutdown(Entity<RMCDeafDisabilityComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<DeafComponent>(ent.Owner);
    }
}
