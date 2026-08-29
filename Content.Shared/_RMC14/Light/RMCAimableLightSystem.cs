using Content.Shared._RMC14.Lights;
using Content.Shared.CombatMode;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Light.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Light;

public sealed class RMCAimableLightSystem : EntitySystem
{
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly PointLightRotationSystem _pointLightRotation = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeAllEvent<RMCAimableLightRotateEvent>(OnRotate);
    }

    private void OnRotate(RMCAimableLightRotateEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var light = GetEntity(ev.Light);
        if (!HasComp<RMCAimableLightComponent>(light) ||
            !TryComp(light, out PointLightRotationComponent? rotation))
        {
            return;
        }

        if (!_hands.IsHolding((user, null), light))
            return;

        if (!_combatMode.IsInCombatMode(user))
            return;

        if (!TryComp(light, out HandheldLightComponent? handheld) || !handheld.Activated)
            return;

        var origin = _transform.GetMapCoordinates(user);
        var target = _transform.ToMapCoordinates(GetCoordinates(ev.Coordinates));
        if (target.MapId != origin.MapId)
            return;

        var direction = target.Position - origin.Position;
        if (direction.LengthSquared() <= 0.0001f)
            return;

        _pointLightRotation.SetRotation(light, direction.ToWorldAngle(), rotation);
    }
}

[Serializable, NetSerializable]
public sealed class RMCAimableLightRotateEvent : EntityEventArgs
{
    public NetEntity Light;
    public NetCoordinates Coordinates;
}
