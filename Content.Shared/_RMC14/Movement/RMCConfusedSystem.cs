using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Movement;

public sealed class RMCConfusedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;

    private static readonly EntProtoId ConfusedStatus = "RMCStatusEffectConfused";

    private const uint RerollTicks = 30;

    private static readonly Vector2[] CardinalDirections =
    [
        new(0, 1),
        new(0, -1),
        new(1, 0),
        new(-1, 0),
    ];

    public override void Initialize()
    {
        SubscribeLocalEvent<InputMoverComponent, ConfusedMovementEvent>(OnConfusedMovement);

        SubscribeLocalEvent<RMCConfusedStatusEffectComponent, StatusEffectAppliedEvent>(OnConfusedApplied);
        SubscribeLocalEvent<RMCConfusedStatusEffectComponent, StatusEffectRemovedEvent>(OnConfusedRemoved);
    }

    private void OnConfusedApplied(Entity<RMCConfusedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _popup.PopupEntity(Loc.GetString("rmc-confused-applied"), args.Target, args.Target);
    }

    private void OnConfusedRemoved(Entity<RMCConfusedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _popup.PopupEntity(Loc.GetString("rmc-confused-removed"), args.Target, args.Target);
    }

    private void OnConfusedMovement(Entity<InputMoverComponent> ent, ref ConfusedMovementEvent args)
    {
        if (args.WishDir == Vector2.Zero)
            return;

        if (!_status.HasStatusEffect(ent.Owner, ConfusedStatus))
            return;

        var bucket = _timing.CurTick.Value / RerollTicks;

        var netId = GetNetEntity(ent.Owner).Id;
        int seed;
        unchecked
        {
            seed = 17;
            seed = seed * 31 + netId;
            seed = seed * 31 + (int) bucket;
        }

        var random = new System.Random(seed);
        var direction = CardinalDirections[random.Next(CardinalDirections.Length)];

        args.WishDir = direction * args.WishDir.Length();
    }
}
