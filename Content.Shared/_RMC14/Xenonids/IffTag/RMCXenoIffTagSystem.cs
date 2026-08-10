using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.IffTag;

public sealed class RMCXenoIffTagSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly TimeSpan ImplantDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RemoveDelay = TimeSpan.FromSeconds(5);

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCXenoIffTagItemComponent, AfterInteractEvent>(OnTagAfterInteract);
        SubscribeLocalEvent<RMCXenoIffTagItemComponent, RMCXenoIffTagDoAfterEvent>(OnTagDoAfter);

        SubscribeLocalEvent<RMCXenoIffTagComponent, GetVerbsEvent<AlternativeVerb>>(OnGetRemoveVerb);
        SubscribeLocalEvent<RMCXenoIffTagComponent, RMCXenoIffTagRemoveDoAfterEvent>(OnRemoveDoAfter);
    }

    private void OnTagAfterInteract(Entity<RMCXenoIffTagItemComponent> tag, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<XenoComponent>(target))
            return;

        if (!TryComp<MobStateComponent>(target, out var mobState) || _mobState.IsDead(target, mobState))
        {
            _popup.PopupClient("You can't implant a tag into a dead Xenonid!", args.User, args.User);
            return;
        }

        if (HasComp<RMCXenoIffTagComponent>(target))
        {
            _popup.PopupClient($"{Name(target)} already has a tag inside it.", args.User, args.User);
            return;
        }

        args.Handled = true;
        _popup.PopupClient($"You start forcing {Name(tag)} into {Name(target)}'s carapace...", args.User, args.User);

        var ev = new RMCXenoIffTagDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, ImplantDelay, ev, tag, target, tag)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnTagDoAfter(Entity<RMCXenoIffTagItemComponent> tag, ref RMCXenoIffTagDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!HasComp<XenoComponent>(target) || HasComp<RMCXenoIffTagComponent>(target))
            return;

        if (!TryComp<MobStateComponent>(target, out var mobState) || _mobState.IsDead(target, mobState))
            return;

        args.Handled = true;

        _popup.PopupClient($"You force {Name(tag)} into {Name(target)}'s carapace!", args.User, args.User);

        var xeno = new Entity<RMCXenoIffTagComponent>(target, EnsureComp<RMCXenoIffTagComponent>(target));

        var buffer = new HashSet<EntProtoId<IFFFactionComponent>>();
        _gunIFF.TryGetFactions(args.User, buffer);
        xeno.Comp.Factions.UnionWith(buffer);
        Dirty(xeno);

        foreach (var faction in xeno.Comp.Factions)
        {
            _gunIFF.AddUserFaction(xeno.Owner, faction);
        }

        QueueDel(tag);
    }

    private void OnGetRemoveVerb(Entity<RMCXenoIffTagComponent> xeno, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = "Remove IFF Tag",
            Act = () =>
            {
                _popup.PopupClient($"You start removing the IFF tag from {Name(xeno)}'s carapace...", user, user);

                var ev = new RMCXenoIffTagRemoveDoAfterEvent();
                var doAfter = new DoAfterArgs(EntityManager, user, RemoveDelay, ev, xeno, xeno)
                {
                    BreakOnMove = true,
                    NeedHand = true,
                };

                _doAfter.TryStartDoAfter(doAfter);
            },
        });
    }

    private void OnRemoveDoAfter(Entity<RMCXenoIffTagComponent> xeno, ref RMCXenoIffTagRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        foreach (var faction in xeno.Comp.Factions)
        {
            _gunIFF.RemoveUserFaction(xeno.Owner, faction);
        }

        RemComp<RMCXenoIffTagComponent>(xeno);

        _popup.PopupClient($"You rip the IFF tag out of {Name(xeno)}'s carapace!", args.User, args.User);
    }
}
