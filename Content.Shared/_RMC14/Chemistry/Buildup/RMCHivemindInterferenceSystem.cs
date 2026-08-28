using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Chemistry.Buildup;

/// <summary>
///     cm13's /datum/component/status_effect/interference. See <see cref="RMCHivemindInterferenceComponent"/>.
///     Whether the hivemind chat channel is actually blocked is checked in SharedChatSystem.
/// </summary>
public sealed class RMCHivemindInterferenceSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    ///     Adds to the entity's hivemind interference buildup, matching cm13's AddComponent/InheritComponent
    ///     stacking - magnitude adds (capped at <paramref name="maxBuildup"/>), while the dissipation rate
    ///     and cap are refreshed to the latest application.
    /// </summary>
    public void AddBuildup(EntityUid uid, float magnitude, float dissipationRate, float maxBuildup)
    {
        if (_net.IsClient)
            return;

        var isNew = !HasComp<RMCHivemindInterferenceComponent>(uid);
        var comp = EnsureComp<RMCHivemindInterferenceComponent>(uid);
        comp.Magnitude = Math.Min(comp.Magnitude + magnitude, maxBuildup);
        comp.DissipationRate = dissipationRate;
        comp.MaxBuildup = maxBuildup;
        Dirty(uid, comp);

        if (isNew)
            _popup.PopupEntity(Loc.GetString("rmc-hivemind-interference-start"), uid, uid, PopupType.LargeCaution);
    }

    /// <summary>
    ///     Seconds until the buildup fully dissipates and the hivemind chat channel unblocks.
    /// </summary>
    public float GetRemainingSeconds(EntityUid uid)
    {
        if (!TryComp<RMCHivemindInterferenceComponent>(uid, out var comp) || comp.DissipationRate <= 0f)
            return 0f;

        return comp.Magnitude / comp.DissipationRate;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<RMCHivemindInterferenceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Magnitude = MathF.Max(comp.Magnitude - comp.DissipationRate * frameTime, 0f);

            if (comp.Magnitude <= 0f)
            {
                RemCompDeferred<RMCHivemindInterferenceComponent>(uid);
                continue;
            }

            Dirty(uid, comp);
        }
    }
}
