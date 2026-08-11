using Content.Shared._RMC14.Ghost;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.RevivalJelly;

public sealed class RMCRevivalJellySystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGhostSystem _ghost = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    private static readonly TimeSpan ReviveDelay = TimeSpan.FromSeconds(3);

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCRevivalJellyComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RMCRevivalJellyComponent, RMCRevivalJellyDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<RMCRevivalJellyComponent> jelly, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<XenoComponent>(target, out var xeno))
            return;

        if (target == args.User)
        {
            _popup.PopupClient("You can't use this on yourself!", args.User, args.User);
            return;
        }

        if (!TryComp<MobStateComponent>(target, out var mobState) || !_mobState.IsDead(target, mobState))
        {
            _popup.PopupClient($"This can only be used on a dead Xenonid!", args.User, args.User);
            return;
        }

        if (!TryComp<StackComponent>(jelly, out var stack))
            return;

        var required = Math.Max(1, xeno.Tier);
        if (stack.Count < required)
        {
            _popup.PopupClient($"There isn't enough jelly to revive this Xenonid! You require {required} to do this.", args.User, args.User);
            return;
        }

        args.Handled = true;
        _popup.PopupClient($"You start applying {Name(jelly)} onto {Name(target)}.", args.User, args.User);

        var doAfterEv = new RMCRevivalJellyDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, ReviveDelay, doAfterEv, jelly, target, jelly)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(Entity<RMCRevivalJellyComponent> jelly, ref RMCRevivalJellyDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!TryComp<XenoComponent>(target, out var xeno) ||
            !TryComp<MobStateComponent>(target, out var mobState) ||
            !_mobState.IsDead(target, mobState))
        {
            return;
        }

        if (!TryComp<StackComponent>(jelly, out var stack))
            return;

        var required = Math.Max(1, xeno.Tier);
        if (stack.Count < required || !_stack.Use(jelly, required, stack))
            return;

        args.Handled = true;

        if (TryComp<DamageableComponent>(target, out var damageable))
            _damageable.SetAllDamage(target, damageable, FixedPoint2.Zero);

        _mobState.ChangeMobState(target, MobState.Alive, mobState, args.User);

        ReattachGhostIfConnected(target);

        if (TryComp<StackComponent>(jelly, out var remaining) && remaining.Count <= 0)
            QueueDel(jelly);
    }

    private void ReattachGhostIfConnected(EntityUid target)
    {
        if (!_mind.TryGetMind(target, out _, out var mind) || mind.UserId is not { } userId)
            return;

        if (!_player.TryGetSessionById(userId, out var session))
            return;

        if (session.AttachedEntity is not { } ghost || !HasComp<GhostComponent>(ghost))
            return;

        var returnTo = EnsureComp<RMCGhostReturnComponent>(ghost);
        returnTo.Target = target;
        Dirty(ghost, returnTo);
        _ghost.SetCanReturnToBody(ghost, true);
    }
}
