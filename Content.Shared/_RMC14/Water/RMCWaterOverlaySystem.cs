using Content.Shared.Movement.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Water;

public sealed class RMCWaterOverlaySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCWaterSystem _rmcWater = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float WadingSoundDistance = 1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCWaterComponent, StartCollideEvent>(OnWaterStartCollide);
        SubscribeLocalEvent<RMCWaterComponent, EndCollideEvent>(OnWaterEndCollide);
        SubscribeLocalEvent<RMCWaterOverlayComponent, MoveEvent>(OnOverlayMove);
    }

    private void OnWaterStartCollide(Entity<RMCWaterComponent> water, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!_rmcWater.CanCollide(water.Owner, other))
            return;

        if (!TryComp(other, out RMCWaterOverlayComponent? overlay))
            return;

        if (overlay.Colliding.Contains(water.Owner))
            return;

        var wasInWater = overlay.InWater;
        overlay.Colliding.Add(water.Owner);
        Dirty(other, overlay);

        if (!wasInWater)
            Log.Info($"[WaterOverlay] {ToPrettyString(other)} entered water via {ToPrettyString(water)}, colliding={overlay.Colliding.Count}");

        RefreshOverlay((other, overlay), forceEvent: !wasInWater);

        if (!wasInWater)
            SetHeavyFootstep(other, true);
    }

    private void OnWaterEndCollide(Entity<RMCWaterComponent> water, ref EndCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!TryComp(other, out RMCWaterOverlayComponent? overlay))
            return;

        if (!overlay.Colliding.Remove(water.Owner))
            return;

        Dirty(other, overlay);
        var stillInWater = overlay.InWater;

        if (!stillInWater)
            Log.Info($"[WaterOverlay] {ToPrettyString(other)} left water (last was {ToPrettyString(water)})");

        RefreshOverlay((other, overlay), forceEvent: !stillInWater);

        if (!stillInWater)
            SetHeavyFootstep(other, false);
    }

    public bool IsInWater(Entity<RMCWaterOverlayComponent?> overlay)
    {
        return Resolve(overlay, ref overlay.Comp, false) && overlay.Comp.InWater;
    }

    /// <inheritdoc cref="IsInWater"/>
    public WaterDepth GetEffectiveDepth(Entity<RMCWaterOverlayComponent?> overlay)
    {
        return Resolve(overlay, ref overlay.Comp, false) ? overlay.Comp.EffectiveDepth : WaterDepth.CoastShallow;
    }

    /// <inheritdoc cref="IsInWater"/>
    public bool IsOverlayToxic(Entity<RMCWaterOverlayComponent?> overlay)
    {
        return Resolve(overlay, ref overlay.Comp, false) && overlay.Comp.Toxic;
    }

    private void RefreshOverlay(Entity<RMCWaterOverlayComponent> overlay, bool forceEvent = false)
    {
        var deepest = WaterDepth.CoastShallow;
        var toxic = false;
        foreach (var water in overlay.Comp.Colliding)
        {
            var depth = _rmcWater.GetDepth(water);
            if (depth > deepest)
                deepest = depth;

            if (_rmcWater.IsToxic(water))
                toxic = true;
        }

        var changed = overlay.Comp.EffectiveDepth != deepest || overlay.Comp.Toxic != toxic;
        if (changed)
        {
            overlay.Comp.EffectiveDepth = deepest;
            overlay.Comp.Toxic = toxic;
            Dirty(overlay);
        }

        if (!changed && !forceEvent)
            return;

        var ev = new WaterOverlayChangedEvent();
        RaiseLocalEvent(overlay.Owner, ref ev);
    }

    private void SetHeavyFootstep(EntityUid uid, bool entering)
    {
        if (!TryComp(uid, out RMCHeavyWaterFootstepComponent? heavy))
            return;

        if (entering)
        {
            if (heavy.Swapped)
                return;

            heavy.HadFootstepModifier = TryComp(uid, out FootstepModifierComponent? existing);
            heavy.PreviousSound = existing?.FootstepSoundCollection;
            heavy.Swapped = true;

            var footstep = EnsureComp<FootstepModifierComponent>(uid);
            footstep.FootstepSoundCollection = heavy.Sound;
            Dirty(uid, footstep);
        }
        else
        {
            if (!heavy.Swapped)
                return;

            heavy.Swapped = false;

            if (heavy.HadFootstepModifier)
            {
                var footstep = EnsureComp<FootstepModifierComponent>(uid);
                footstep.FootstepSoundCollection = heavy.PreviousSound;
                Dirty(uid, footstep);
            }
            else
            {
                RemComp<FootstepModifierComponent>(uid);
            }

            heavy.PreviousSound = null;
        }
    }

    private void OnOverlayMove(Entity<RMCWaterOverlayComponent> ent, ref MoveEvent args)
    {
        if (!ent.Comp.InWater)
            return;

        if (args.NewRotation != args.OldRotation)
        {
            var ev = new WaterOverlayChangedEvent();
            RaiseLocalEvent(ent.Owner, ref ev);
        }

        if (_timing.ApplyingState)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp(ent, out InputMoverComponent? mover) || !mover.Sprinting)
            return;

        if (args.OldPosition.EntityId != args.NewPosition.EntityId)
            return;

        var distance = (args.NewPosition.Position - args.OldPosition.Position).Length();
        ent.Comp.DistanceSinceWadingSound += distance;
        if (ent.Comp.DistanceSinceWadingSound < WadingSoundDistance)
            return;

        ent.Comp.DistanceSinceWadingSound = 0f;

        var sound = ent.Comp.EffectiveDepth switch
        {
            WaterDepth.Deep => WaterSounds.WadingDeep,
            WaterDepth.Shallow => WaterSounds.WadingMid,
            _ => WaterSounds.WadingShallow,
        };

        _audio.PlayPredicted(sound, ent, ent);
    }
}
