using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.TacticalMap;

public sealed class TacMapLiveUpdateSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapUserComponent, ComponentStartup>(OnUserStartup);
        SubscribeLocalEvent<GrantTacMapLiveUpdateComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<GrantTacMapLiveUpdateComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnUserStartup(Entity<TacticalMapUserComponent> ent, ref ComponentStartup args)
    {
        RefreshLiveUpdate(ent);
    }

    private void OnEquipped(Entity<GrantTacMapLiveUpdateComponent> ent, ref GotEquippedEvent args)
    {
        if ((ent.Comp.Slots & args.SlotFlags) != 0 &&
            TryComp(args.Equipee, out TacticalMapUserComponent? user))
        {
            RefreshLiveUpdate((args.Equipee, user));
        }
    }

    private void OnUnequipped(Entity<GrantTacMapLiveUpdateComponent> ent, ref GotUnequippedEvent args)
    {
        if ((ent.Comp.Slots & args.SlotFlags) != 0 &&
            TryComp(args.Equipee, out TacticalMapUserComponent? user))
        {
            RefreshLiveUpdate((args.Equipee, user));
        }
    }

    public void RefreshLiveUpdate(Entity<TacticalMapUserComponent> user)
    {
        if (_timing.ApplyingState)
            return;

        user.Comp.IntrinsicLiveUpdate ??= user.Comp.LiveUpdate;
        var live = user.Comp.IntrinsicLiveUpdate.Value;
        var slots = _inventory.GetSlotEnumerator(user.Owner);
        while (slots.NextItem(out var item, out var slot))
        {
            if (TryComp(item, out GrantTacMapLiveUpdateComponent? grant) &&
                (grant.Slots & slot.SlotFlags) != 0)
            {
                live = true;
                break;
            }
        }

        if (TryComp(user, out TacticalMapLiveUpdateOnOviComponent? ovi) && ovi.Enabled)
            live |= HasHiveOvipositor(user.Owner);

        if (user.Comp.LiveUpdate == live)
            return;

        user.Comp.LiveUpdate = live;
        Dirty(user);
    }

    private bool HasHiveOvipositor(EntityUid user)
    {
        if (!TryComp<HiveMemberComponent>(user, out var member) || member.Hive is not { } hive)
            return false;

        var queens = EntityQueryEnumerator<XenoAttachedOvipositorComponent, HiveMemberComponent>();
        while (queens.MoveNext(out var queen, out _, out var queenHive))
        {
            if (queenHive.Hive == hive && !TerminatingOrDeleted(queen) && !_mobState.IsDead(queen))
                return true;
        }

        return false;
    }
}
