using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry;
using Content.Shared.Clothing;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Spillable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    protected override void InitializeSpillable()
    {
        base.InitializeSpillable();

        SubscribeLocalEvent<SpillableComponent, LandEvent>(SpillOnLand);
        // Openable handles the event if it's closed
        SubscribeLocalEvent<SpillableComponent, MeleeHitEvent>(SplashOnMeleeHit, after: [typeof(OpenableSystem)]);
        SubscribeLocalEvent<SpillableComponent, SolutionContainerOverflowEvent>(OnOverflow);
        SubscribeLocalEvent<SpillableComponent, SpillDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SpillableComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
    }

    private void OnOverflow(Entity<SpillableComponent> entity, ref SolutionContainerOverflowEvent args)
    {
        if (args.Handled)
            return;

        TrySpillAt(Transform(entity).Coordinates, args.Overflow, out _);
        args.Handled = true;
    }

    private void SplashOnMeleeHit(Entity<SpillableComponent> entity, ref MeleeHitEvent args)
    {
        if (args.Handled)
            return;

        if (!_solutionContainerSystem.TryGetDrainableSolution(entity.Owner, out var soln, out var solution) ||
            solution.Volume <= FixedPoint2.Zero)
        {
            return;
        }

        if (args.HitEntities.Count == 0)
        {
            args.Handled = entity.Comp.PreventMelee;

            var splashedOnGround = _solutionContainerSystem.SplitSolution(soln.Value, solution.Volume);
            TrySplashSpillAt(entity.Owner, Transform(args.User).Coordinates, splashedOnGround, out _);
            return;
        }

        // Optionally allow further melee handling occur
        args.Handled = entity.Comp.PreventMelee;

        var hitCount = args.HitEntities.Count;
        var totalSplit = solution.Volume;

        foreach (var hit in args.HitEntities)
        {
            var splitSolution = _solutionContainerSystem.SplitSolution(soln.Value, totalSplit / hitCount);

            if (!HasComp<ReactiveComponent>(hit))
                continue;

            _adminLogger.Add(LogType.MeleeHit, $"{ToPrettyString(args.User)} splashed {SharedSolutionContainerSystem.ToPrettyString(splitSolution):solution} from {ToPrettyString(entity.Owner):entity} onto {ToPrettyString(hit):target}");
            _reactive.DoEntityReaction(hit, splitSolution, ReactionMethod.Touch);

            _popups.PopupEntity(
                Loc.GetString("spill-melee-hit-attacker", ("amount", totalSplit / hitCount), ("spillable", entity.Owner),
                    ("target", Identity.Entity(hit, EntityManager))),
                hit, args.User);

            _popups.PopupEntity(
                Loc.GetString("spill-melee-hit-others", ("attacker", args.User), ("spillable", entity.Owner),
                    ("target", Identity.Entity(hit, EntityManager))),
                hit, Filter.PvsExcept(args.User), true, PopupType.SmallCaution);
        }
    }

    private void SpillOnLand(Entity<SpillableComponent> entity, ref LandEvent args)
    {
        if (!_solutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out var soln, out var solution))
            return;

        if (Openable.IsClosed(entity.Owner))
            return;

        if (!entity.Comp.SpillWhenThrown)
            return;

        if (args.User != null)
        {
            _adminLogger.Add(LogType.Landed,
                $"{ToPrettyString(entity.Owner):entity} spilled a solution {SharedSolutionContainerSystem.ToPrettyString(solution):solution} on landing");
        }

        var drainedSolution = _solutionContainerSystem.Drain(entity.Owner, soln.Value, solution.Volume);
        TrySplashSpillAt(entity.Owner, Transform(entity).Coordinates, drainedSolution, out _);
    }

    /// <summary>
    /// Prevent Pacified entities from throwing items that can spill liquids.
    /// </summary>
    private void OnAttemptPacifiedThrow(Entity<SpillableComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        // Don’t care about closed containers.
        if (Openable.IsClosed(ent))
            return;

        // Don’t care about empty containers.
        if (!_solutionContainerSystem.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out _, out var solution) || solution.Volume <= 0)
            return;

        args.Cancel("pacified-cannot-throw-spill");
    }

    private void OnDoAfter(Entity<SpillableComponent> entity, ref SpillDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        //solution gone by other means before doafter completes
        if (!_solutionContainerSystem.TryGetDrainableSolution(entity.Owner, out var soln, out var solution) || solution.Volume == 0)
            return;

        var puddleSolution = _solutionContainerSystem.SplitSolution(soln.Value, solution.Volume);
        TrySpillAt(Transform(entity).Coordinates, puddleSolution, out _);
        args.Handled = true;
    }
}
