using Content.Shared._RMC14.Drugs;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client._RMC14.Drugs;

public sealed class RMCDrugOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private RMCRainbowOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSeeingRainbowsStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<RMCSeeingRainbowsStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);

        SubscribeLocalEvent<RMCSeeingRainbowsStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerAttachedEvent>>(OnPlayerAttached);
        SubscribeLocalEvent<RMCSeeingRainbowsStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerDetachedEvent>>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnRemoved(Entity<RMCSeeingRainbowsStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        _overlay.Intoxication = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnApplied(Entity<RMCSeeingRainbowsStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        _overlay.Phase = _random.NextFloat(MathF.Tau);
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerAttached(Entity<RMCSeeingRainbowsStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<RMCSeeingRainbowsStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        _overlay.Intoxication = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }
}
