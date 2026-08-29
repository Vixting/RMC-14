using System;
using System.Collections.Generic;
using Content.Client.CombatMode;
using Content.Shared._RMC14.Light;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Light.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Light;

public sealed class RMCAimableLightInputSystem : EntitySystem
{
    private const float AimUpdateInterval = 0.02f;
    private static readonly Angle AimEpsilon = Angle.FromDegrees(1);

    [Dependency] private readonly CombatModeSystem _combat = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, (Angle Angle, TimeSpan Time)> _lastAims = new();

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } user)
            return;

        if (!_combat.IsInCombatMode(user))
            return;

        if (!_input.MouseScreenPosition.IsValid)
            return;

        if (!TryComp(user, out HandsComponent? hands))
            return;

        var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        var origin = _transform.GetMapCoordinates(user);
        var direction = mousePos.Position - origin.Position;
        if (direction.LengthSquared() <= 0.0001f)
            return;

        var angle = direction.ToWorldAngle();
        var mouseCoords = GetNetCoordinates(_transform.ToCoordinates(mousePos));

        foreach (var held in _hands.EnumerateHeld((user, hands)))
        {
            if (!HasComp<RMCAimableLightComponent>(held))
                continue;

            if (!TryComp(held, out HandheldLightComponent? handheld) || !handheld.Activated)
                continue;

            if (_lastAims.TryGetValue(held, out var last) &&
                (_timing.CurTime - last.Time).TotalSeconds < AimUpdateInterval &&
                Math.Abs(Angle.ShortestDistance(angle, last.Angle).Degrees) < AimEpsilon.Degrees)
            {
                continue;
            }

            _lastAims[held] = (angle, _timing.CurTime);

            RaisePredictiveEvent(new RMCAimableLightRotateEvent
            {
                Light = GetNetEntity(held),
                Coordinates = mouseCoords,
            });
        }
    }
}
