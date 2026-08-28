using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

/// <summary>
///     Drives the shared <see cref="BlindableComponent"/> vision floor for the Blind and
///     Nearsighted disabilities (cm13 prop_neutral.dm neuroinhibiting, prop_negative.dm oculotoxic).
///     They're handled together because they compete for the same underlying floor - full
///     blindness always wins over a nearsighted blur - rather than fighting over the field from two
///     separate systems.
/// </summary>
public sealed class RMCVisionDisabilitySystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blinding = default!;

    // cm13: nearsighted blur sits below the full-blindness threshold (BlindableComponent.MaxDamage)
    private const int NearsightedFloor = 4;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCBlindDisabilityComponent, ComponentStartup>(OnBlindStartup);
        SubscribeLocalEvent<RMCBlindDisabilityComponent, ComponentShutdown>(OnBlindShutdown);
        SubscribeLocalEvent<RMCNearsightedComponent, ComponentStartup>(OnNearsightedStartup);
        SubscribeLocalEvent<RMCNearsightedComponent, ComponentShutdown>(OnNearsightedShutdown);
    }

    private void OnBlindStartup(Entity<RMCBlindDisabilityComponent> ent, ref ComponentStartup args)
    {
        Apply(ent.Owner, blind: true);
    }

    private void OnBlindShutdown(Entity<RMCBlindDisabilityComponent> ent, ref ComponentShutdown args)
    {
        Apply(ent.Owner, blind: false);
    }

    private void OnNearsightedStartup(Entity<RMCNearsightedComponent> ent, ref ComponentStartup args)
    {
        Apply(ent.Owner, blind: HasComp<RMCBlindDisabilityComponent>(ent.Owner), nearsighted: true);
    }

    private void OnNearsightedShutdown(Entity<RMCNearsightedComponent> ent, ref ComponentShutdown args)
    {
        Apply(ent.Owner, blind: HasComp<RMCBlindDisabilityComponent>(ent.Owner), nearsighted: false);
    }

    /// <summary>
    ///     Recomputes the vision floor from explicit blind/nearsighted state rather than re-querying
    ///     the component that's mid-Startup/Shutdown - HasComp on that component during its own
    ///     lifecycle event isn't something to rely on, so the caller tells us what it's becoming.
    /// </summary>
    private void Apply(EntityUid uid, bool blind, bool? nearsighted = null)
    {
        if (!TryComp<BlindableComponent>(uid, out var blindable))
            return;

        var isNearsighted = nearsighted ?? HasComp<RMCNearsightedComponent>(uid);
        var floor = blind ? blindable.MaxDamage : isNearsighted ? NearsightedFloor : 0;
        _blinding.SetMinDamage((uid, blindable), floor);
    }
}
