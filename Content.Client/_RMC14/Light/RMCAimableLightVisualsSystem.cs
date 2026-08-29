using System;
using System.Numerics;
using Content.Client.CombatMode;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Lights;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Light.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Light;

public sealed class RMCAimableLightVisualsSystem : EntitySystem
{
    private const double RotationSpeedDegrees = 100.0;
    private const double RotationEaseRate = 10.0;
    private const float OffsetDistance = 0.6f;

    [Dependency] private readonly CombatModeSystem _combat = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCAimableLightComponent, PointLightRotationComponent, PointLightComponent>();
        while (query.MoveNext(out var uid, out _, out var rotation, out var light))
        {
            var held = TryGetHolder(uid, out var holder);
            var aiming = held && _combat.IsInCombatMode(holder);
            var goal = aiming ? rotation.Rotation : Angle.Zero;

            var delta = Angle.ShortestDistance(light.Rotation, goal);
            var remaining = Math.Abs(delta.Theta);

            var maxStep = MathHelper.DegreesToRadians(RotationSpeedDegrees) * frameTime;
            var easedStep = remaining * (1.0 - Math.Exp(-RotationEaseRate * frameTime));
            var step = Math.Min(maxStep, easedStep);

            var newRotation = remaining <= step
                ? goal
                : new Angle(light.Rotation.Theta + Math.Sign(delta.Theta) * step);

#pragma warning disable RA0002
            light.Rotation = newRotation;
            light.MaskAutoRotate = !aiming;

            if (aiming)
            {
                var bodyRot = _transform.GetWorldRotation(holder);
                light.Offset = (-bodyRot).RotateVec(newRotation.ToWorldVec() * OffsetDistance);
            }
            else
            {
                light.Offset = Vector2.Zero;
            }
#pragma warning restore RA0002
        }
    }

    private bool TryGetHolder(EntityUid light, out EntityUid holder)
    {
        holder = default;

        if (!TryComp(light, out HandheldLightComponent? handheld) || !handheld.Activated)
            return false;

        if (!_container.TryGetContainingContainer((light, null, null), out var container))
            return false;

        holder = container.Owner;
        return _hands.IsHolding((holder, null), light);
    }
}
