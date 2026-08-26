using Content.Server.Fluids.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._RMC14.Fluids;
using Content.Shared._RMC14.Synth;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server._RMC14.Fluids;

public sealed class RMCBackpackSprayerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCBackpackSprayerComponent, GetVerbsEvent<AlternativeVerb>>(OnBackpackGetAltVerbs);
        SubscribeLocalEvent<RMCBackpackSprayerComponent, GotUnequippedEvent>(OnBackpackUnequipped);
        SubscribeLocalEvent<RMCBackpackSprayerComponent, DroppedEvent>(OnBackpackDropped);

        SubscribeLocalEvent<RMCSprayerNozzleComponent, SprayAttemptEvent>(OnNozzleSprayAttempt);
        SubscribeLocalEvent<RMCSprayerNozzleComponent, DroppedEvent>(OnNozzleDropped);

        SubscribeLocalEvent<RMCFirefighterNozzleComponent, GetVerbsEvent<AlternativeVerb>>(OnFirefighterGetAltVerbs);
        SubscribeLocalEvent<RMCFirefighterNozzleComponent, SprayAttemptEvent>(OnFirefighterSprayAttempt);
        SubscribeLocalEvent<RMCFirefighterNozzleComponent, AfterInteractEvent>(OnFirefighterAfterInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var pending = EntityQueryEnumerator<PendingFoamBallComponent>();
        while (pending.MoveNext(out var uid, out var ball))
        {
            var frac = ball.Duration > TimeSpan.Zero
                ? Math.Clamp((float) ((time - ball.StartTime) / ball.Duration), 0f, 1f)
                : 1f;

            if (frac >= 1f)
            {
                QueueDel(uid);
                SpawnFoam(ball.FoamPrototype, ball.TargetCoordinates, ball.Spread, ball.LandSound);
                continue;
            }

            var pos = Vector2.Lerp(ball.Start.Position, ball.Target.Position, frac);
            _transform.SetMapCoordinates(uid, new MapCoordinates(pos, ball.Start.MapId));
        }
    }

    private void OnBackpackGetAltVerbs(Entity<RMCBackpackSprayerComponent> backpack, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-backpack-sprayer-toggle-nozzle"),
            Act = () => ToggleNozzle(backpack, user),
        });
    }

    private void OnBackpackUnequipped(Entity<RMCBackpackSprayerComponent> backpack, ref GotUnequippedEvent args)
    {
        RetractNozzle(backpack);
    }

    private void OnBackpackDropped(Entity<RMCBackpackSprayerComponent> backpack, ref DroppedEvent args)
    {
        RetractNozzle(backpack);
    }

    private void OnNozzleDropped(Entity<RMCSprayerNozzleComponent> nozzle, ref DroppedEvent args)
    {
        if (nozzle.Comp.Backpack is not { } backpackUid ||
            !TryComp<RMCBackpackSprayerComponent>(backpackUid, out var backpackComp))
        {
            return;
        }

        RetractNozzle((backpackUid, backpackComp));
    }

    private void ToggleNozzle(Entity<RMCBackpackSprayerComponent> backpack, EntityUid user)
    {
        if (!_inventory.TryGetSlotEntity(user, "back", out var worn) || worn != backpack.Owner)
        {
            _popup.PopupEntity(Loc.GetString("rmc-backpack-sprayer-must-be-worn"), backpack, user);
            return;
        }

        if (backpack.Comp.Nozzle is { } existing && Exists(existing))
        {
            RetractNozzle(backpack);
            return;
        }

        var nozzle = Spawn(backpack.Comp.NozzlePrototype, Transform(user).Coordinates);
        var nozzleComp = EnsureComp<RMCSprayerNozzleComponent>(nozzle);
        nozzleComp.Backpack = backpack;
        Dirty(nozzle, nozzleComp);

        if (!_hands.TryPickupAnyHand(user, nozzle))
        {
            _popup.PopupEntity(Loc.GetString("rmc-backpack-sprayer-no-free-hand"), backpack, user);
            QueueDel(nozzle);
            return;
        }

        backpack.Comp.Nozzle = nozzle;
        Dirty(backpack);
        _appearance.SetData(backpack, RMCBackpackSprayerVisuals.NozzleAttached, false);
    }

    private void RetractNozzle(Entity<RMCBackpackSprayerComponent> backpack)
    {
        if (backpack.Comp.Nozzle is { } nozzle)
            QueueDel(nozzle);

        backpack.Comp.Nozzle = null;
        Dirty(backpack);
        _appearance.SetData(backpack, RMCBackpackSprayerVisuals.NozzleAttached, true);
    }

    private void OnNozzleSprayAttempt(Entity<RMCSprayerNozzleComponent> nozzle, ref SprayAttemptEvent args)
    {
        if (nozzle.Comp.Backpack is not { } backpack || !Exists(backpack))
        {
            args.Cancel();
            return;
        }

        if (!TryComp<RMCBackpackSprayerComponent>(backpack, out var backpackComp))
        {
            args.Cancel();
            return;
        }

        if (!_solutionContainer.TryGetSolution((backpack, null), backpackComp.Solution, out var tankSoln, out var tankSolution))
        {
            args.Cancel();
            return;
        }

        if (!_solutionContainer.TryGetSolution((nozzle.Owner, null), SprayComponent.SolutionName, out var nozzleSoln, out var nozzleSolution))
        {
            args.Cancel();
            return;
        }

        var missing = nozzleSolution.AvailableVolume;
        var amount = FixedPoint2.Min(nozzle.Comp.RefillAmount, missing, tankSolution.Volume);
        if (amount <= FixedPoint2.Zero)
        {
            if (tankSolution.Volume <= FixedPoint2.Zero && nozzleSolution.Volume <= FixedPoint2.Zero)
            {
                _popup.PopupEntity(Loc.GetString("rmc-backpack-sprayer-tank-empty"), backpack, args.User);
                args.Cancel();
            }

            return;
        }

        var split = _solutionContainer.SplitSolution(tankSoln.Value, amount);
        _solutionContainer.TryAddSolution(nozzleSoln.Value, split);
    }

    private void OnFirefighterGetAltVerbs(Entity<RMCFirefighterNozzleComponent> nozzle, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-firefighter-nozzle-cycle-mode"),
            Act = () => CycleMode(nozzle, user),
        });
    }

    private void CycleMode(Entity<RMCFirefighterNozzleComponent> nozzle, EntityUid user)
    {
        var next = nozzle.Comp.Mode switch
        {
            RMCFirefighterNozzleMode.Extinguisher => RMCFirefighterNozzleMode.MetalFoamLauncher,
            RMCFirefighterNozzleMode.MetalFoamLauncher => RMCFirefighterNozzleMode.MetalFoamer,
            _ => RMCFirefighterNozzleMode.Extinguisher,
        };

        nozzle.Comp.Mode = next;
        Dirty(nozzle);

        _appearance.SetData(nozzle, RMCFirefighterNozzleVisuals.Mode, next);

        var (text, cost) = next switch
        {
            RMCFirefighterNozzleMode.Extinguisher => ("rmc-firefighter-nozzle-mode-extinguisher", nozzle.Comp.ExtinguisherCost),
            RMCFirefighterNozzleMode.MetalFoamLauncher => ("rmc-firefighter-nozzle-mode-launcher", nozzle.Comp.LauncherCost),
            _ => ("rmc-firefighter-nozzle-mode-foamer", nozzle.Comp.FoamerCost),
        };

        _popup.PopupEntity(Loc.GetString(text, ("cost", cost)), nozzle, user);
    }

    private void OnFirefighterSprayAttempt(Entity<RMCFirefighterNozzleComponent> nozzle, ref SprayAttemptEvent args)
    {
        if (nozzle.Comp.Mode != RMCFirefighterNozzleMode.Extinguisher)
        {
            args.Cancel();
            return;
        }

        if (!HasComp<SynthComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("rmc-firefighter-nozzle-synth-only", ("tool", nozzle.Owner)), nozzle, args.User);
            args.Cancel();
        }
    }

    private void OnFirefighterAfterInteract(Entity<RMCFirefighterNozzleComponent> nozzle, ref AfterInteractEvent args)
    {
        var comp = nozzle.Comp;

        if (comp.Mode == RMCFirefighterNozzleMode.Extinguisher)
            return;

        if (!HasComp<SynthComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("rmc-firefighter-nozzle-synth-only", ("tool", nozzle.Owner)), nozzle, args.User);
            return;
        }

        if (_timing.CurTime < comp.NextUse)
            return;

        if (!TryComp<RMCSprayerNozzleComponent>(nozzle.Owner, out var linkComp) ||
            linkComp.Backpack is not { } backpackUid ||
            !TryComp<RMCBackpackSprayerComponent>(backpackUid, out var backpackComp))
        {
            return;
        }

        var backpack = new Entity<RMCBackpackSprayerComponent>(backpackUid, backpackComp);
        var ranged = comp.Mode == RMCFirefighterNozzleMode.MetalFoamLauncher;

        if (ranged == args.CanReach)
            return;

        var startMap = _transform.ToMapCoordinates(Transform(nozzle.Owner).Coordinates);
        var targetMap = _transform.ToMapCoordinates(args.ClickLocation);
        if (ranged && startMap.MapId != targetMap.MapId)
            return;

        var cost = ranged ? comp.LauncherCost : comp.FoamerCost;
        if (!TryConsumeTank(backpack, cost, args.User))
            return;

        args.Handled = true;
        comp.NextUse = _timing.CurTime + (ranged ? comp.LauncherDelay : comp.FoamerDelay);
        Dirty(nozzle);

        if (ranged)
        {
            _audio.PlayPvs(comp.LaunchSound, nozzle, comp.LaunchSound.Params.WithVariation(0.1f));

            var ball = Spawn(comp.BallPrototype, Transform(nozzle.Owner).Coordinates);
            AddComp(ball, new PendingFoamBallComponent
            {
                Start = startMap,
                Target = targetMap,
                TargetCoordinates = args.ClickLocation,
                StartTime = _timing.CurTime,
                Duration = comp.LauncherTravelTime,
                FoamPrototype = comp.FoamPrototype,
                Spread = comp.LauncherSpread,
                LandSound = comp.FoamSound,
            });
            return;
        }

        SpawnFoam(comp.FoamPrototype, args.ClickLocation, comp.FoamerSpread, comp.FoamSound);
    }

    private bool TryConsumeTank(Entity<RMCBackpackSprayerComponent> backpack, FixedPoint2 cost, EntityUid user)
    {
        if (!_solutionContainer.TryGetSolution((backpack.Owner, null), backpack.Comp.Solution, out var tankSoln, out var tankSolution) ||
            tankSolution.Volume < cost)
        {
            _popup.PopupEntity(Loc.GetString("rmc-backpack-sprayer-tank-empty"), backpack, user);
            return false;
        }

        _solutionContainer.SplitSolution(tankSoln.Value, cost);
        return true;
    }

    private void SpawnFoam(EntProtoId prototype, EntityCoordinates coords, int spreadAmount, SoundSpecifier sound)
    {
        var ent = Spawn(prototype, coords);
        var solution = new Solution("RMCAluminum", FixedPoint2.New(30));
        _smoke.StartSmoke(ent, solution, 10f, spreadAmount);

        _audio.PlayPvs(sound, ent, sound.Params.WithVariation(0.1f));
    }
}
