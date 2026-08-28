using Content.Shared._RMC14.Synth;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Buildup;

/// <summary>
///     cm13's /datum/component/status_effect/healing_reduction. See <see cref="RMCHealingReductionComponent"/>.
/// </summary>
public sealed class RMCHealingReductionSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    ///     Adds to the entity's healing-reduction buildup, matching cm13's AddComponent/InheritComponent
    ///     stacking - magnitude adds (capped at <paramref name="maxBuildup"/>), while the dissipation rate
    ///     and cap are refreshed to the latest application.
    /// </summary>
    public void AddBuildup(EntityUid uid, float magnitude, float dissipationRate, float maxBuildup)
    {
        if (_net.IsClient)
            return;

        var comp = EnsureComp<RMCHealingReductionComponent>(uid);
        comp.Magnitude = Math.Min(comp.Magnitude + magnitude, maxBuildup);
        comp.DissipationRate = dissipationRate;
        comp.MaxBuildup = maxBuildup;
        Dirty(uid, comp);
    }

    /// <summary>
    ///     Returns the entity's current healing-reduction magnitude, or 0 if it has none. Used to reduce
    ///     incoming xeno heals, matching cm13's healing["healing"] -= healing_reduction.
    /// </summary>
    public float GetReduction(EntityUid uid)
    {
        return TryComp<RMCHealingReductionComponent>(uid, out var comp) ? comp.Magnitude : 0f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<RMCHealingReductionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Magnitude = MathF.Max(comp.Magnitude - comp.DissipationRate * frameTime, 0f);

            if (comp.Magnitude <= 0f)
            {
                RemCompDeferred<RMCHealingReductionComponent>(uid);
                continue;
            }

            // cm13: ishuman(parent) - deals brute damage to humans equal to the raw dissipation rate,
            // not scaled by the current magnitude, for as long as any buildup remains.
            if (HasComp<HumanoidAppearanceComponent>(uid) && !HasComp<SynthComponent>(uid))
            {
                var damage = new DamageSpecifier(_prototype.Index(BruteGroup), comp.DissipationRate * frameTime);
                _damageable.TryChangeDamage(uid, damage, true, interruptsDoAfters: false);
            }

            Dirty(uid, comp);
        }
    }
}
