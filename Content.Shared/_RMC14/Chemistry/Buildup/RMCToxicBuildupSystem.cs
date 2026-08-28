using Content.Shared._RMC14.Armor;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Chemistry.Buildup;

/// <summary>
///     cm13's /datum/component/status_effect/toxic_buildup. See <see cref="RMCToxicBuildupComponent"/>.
/// </summary>
public sealed class RMCToxicBuildupSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCToxicBuildupComponent, CMGetArmorEvent>(OnGetArmor);
    }

    // cm13: damagedata["armor"] = max(damagedata["armor"] - toxic_buildup, 0), applied on
    // COMSIG_XENO_PRE_CALCULATE_ARMOURED_DAMAGE_PROJECTILE / COMSIG_XENO_PRE_APPLY_ARMOURED_DAMAGE.
    // CMGetArmorEvent/CMArmorSystem.ModifyDamage is RMC-14's equivalent armor-lookup point - it's raised
    // on the target before the armor value feeds into the 1.1^(armor/5) resist formula, and the result is
    // clamped back to >= 0 there, so subtracting the buildup magnitude here reproduces the same shape.
    private void OnGetArmor(Entity<RMCToxicBuildupComponent> ent, ref CMGetArmorEvent args)
    {
        args.XenoArmor -= (int) ent.Comp.Magnitude;
    }

    /// <summary>
    ///     Adds to the entity's toxic buildup, matching cm13's AddComponent/InheritComponent stacking -
    ///     magnitude adds (capped at <paramref name="maxBuildup"/>), while the dissipation rate and cap
    ///     are refreshed to the latest application.
    /// </summary>
    public void AddBuildup(EntityUid uid, float magnitude, float dissipationRate, float maxBuildup)
    {
        if (_net.IsClient)
            return;

        var comp = EnsureComp<RMCToxicBuildupComponent>(uid);
        comp.Magnitude = Math.Min(comp.Magnitude + magnitude, maxBuildup);
        comp.DissipationRate = dissipationRate;
        comp.MaxBuildup = maxBuildup;
        Dirty(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<RMCToxicBuildupComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Magnitude = MathF.Max(comp.Magnitude - comp.DissipationRate * frameTime, 0f);

            if (comp.Magnitude <= 0f)
            {
                RemCompDeferred<RMCToxicBuildupComponent>(uid);
                continue;
            }

            Dirty(uid, comp);
        }
    }
}
