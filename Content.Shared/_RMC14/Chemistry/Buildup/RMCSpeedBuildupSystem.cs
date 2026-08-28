using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Stamina;
using Content.Shared.Movement.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Chemistry.Buildup;

/// <summary>
///     cm13's /datum/component/status_effect/speed_modifier. See <see cref="RMCSpeedBuildupComponent"/>.
/// </summary>
public sealed class RMCSpeedBuildupSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly RMCStaminaSystem _stamina = default!;
    [Dependency] private readonly TemporarySpeedModifiersSystem _temporarySpeed = default!;

    // cm13: speeds["speed"] += speed_modifier * 0.075 (slow) or -= speed_modifier * 0.1 (boost)
    private const float SlowFactor = 0.075f;
    private const float BoostFactor = 0.1f;

    // cm13 HUMAN_STAMINA_MULTIPLIER
    private const float StaminaMultiplier = 5f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCSpeedBuildupComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovement);
    }

    private void OnRefreshMovement(Entity<RMCSpeedBuildupComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var raw = ent.Comp.IncreaseSpeed ? -ent.Comp.Magnitude * BoostFactor : ent.Comp.Magnitude * SlowFactor;
        if (_temporarySpeed.CalculateSpeedModifier(ent, raw) is not { } multiplier)
            return;

        args.ModifySpeed(multiplier, multiplier);
    }

    /// <summary>
    ///     Adds to the entity's speed buildup, matching cm13's AddComponent/InheritComponent stacking -
    ///     magnitude adds (capped at <paramref name="maxBuildup"/>), while the dissipation rate, cap, and
    ///     buff/debuff direction are refreshed to the latest application.
    /// </summary>
    public void AddBuildup(EntityUid uid, float magnitude, float dissipationRate, float maxBuildup, bool increaseSpeed)
    {
        if (_net.IsClient)
            return;

        var comp = EnsureComp<RMCSpeedBuildupComponent>(uid);
        comp.Magnitude = Math.Min(comp.Magnitude + magnitude, maxBuildup);
        comp.DissipationRate = dissipationRate;
        comp.MaxBuildup = maxBuildup;
        comp.IncreaseSpeed = increaseSpeed;
        Dirty(uid, comp);
        _speed.RefreshMovementSpeedModifiers(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<RMCSpeedBuildupComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Magnitude = MathF.Max(comp.Magnitude - comp.DissipationRate * frameTime, 0f);

            if (TryComp<RMCStaminaComponent>(uid, out var stamina))
            {
                var amount = StaminaMultiplier * comp.DissipationRate * frameTime;
                _stamina.DoStaminaDamage((uid, stamina), comp.IncreaseSpeed ? -amount : amount, false);
            }

            if (comp.Magnitude <= 0f)
            {
                RemCompDeferred<RMCSpeedBuildupComponent>(uid);
                _speed.RefreshMovementSpeedModifiers(uid);
                continue;
            }

            Dirty(uid, comp);
        }
    }
}
