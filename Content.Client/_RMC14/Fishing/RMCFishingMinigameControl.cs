using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Fishing;

public sealed class RMCFishingMinigameControl : Control
{
    private const float BarHeightFrac = 0.14f;
    private const float FishRadius = 6f;
    private const float Padding = 8f;
    private const float ProgressBarWidth = 16f;
    private const float Gap = 6f;

    private readonly IRobustRandom _random;

    private float _fishPos = 0.5f;
    private float _fishVel;
    private float _fishTarget = 0.5f;
    private float _fishTargetTimer;

    private float _barPos = 0.5f;
    private float _barVel;

    private float _progress = 0.5f;

    private float _difficulty = 1f;
    private int _token;
    private bool _active;
    private bool _finished;
    private bool _holding;

    public event Action<bool, int>? OnResult;

    public RMCFishingMinigameControl()
    {
        _random = IoCManager.Resolve<IRobustRandom>();
        MinSize = new Vector2(118, 280);
        MouseFilter = MouseFilterMode.Stop;
    }

    public void Start(float difficulty, int token)
    {
        _difficulty = Math.Clamp(difficulty, 0.5f, 5f);
        _token = token;
        _fishPos = 0.5f;
        _fishVel = 0f;
        _fishTarget = 0.5f;
        _fishTargetTimer = 0f;
        _barPos = 0.5f;
        _barVel = 0f;
        _progress = 0.5f;
        _active = true;
        _finished = false;
        _holding = false;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (_active && args.Function == EngineKeyFunctions.UIClick)
        {
            _holding = true;
            args.Handle();
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        if (args.Function == EngineKeyFunctions.UIClick)
            _holding = false;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (!_active || _finished)
            return;

        var dt = args.DeltaSeconds;

        _fishTargetTimer -= dt;
        if (_fishTargetTimer <= 0f)
        {
            _fishTarget = _random.NextFloat(0.08f, 0.92f);
            _fishTargetTimer = _random.NextFloat(0.4f, 1.3f) / _difficulty;
        }

        _fishVel += (_fishTarget - _fishPos) * 5f * _difficulty * dt;
        _fishVel *= MathF.Pow(0.25f, dt * 6f);
        _fishPos = Math.Clamp(_fishPos + _fishVel * dt, 0f, 1f);

        const float gravity = 1.2f;
        const float lift = 2.8f;
        _barVel += (_holding ? -lift : gravity) * dt;
        _barVel *= MathF.Pow(0.15f, dt * 6f);
        var half = BarHeightFrac / 2f;
        _barPos = Math.Clamp(_barPos + _barVel * dt, half, 1f - half);

        var inside = MathF.Abs(_fishPos - _barPos) < half;
        _progress += (inside ? 0.30f : -0.40f) * dt;
        _progress = Math.Clamp(_progress, 0f, 1f);

        if (_progress >= 1f)
            Finish(true);
        else if (_progress <= 0f)
            Finish(false);
    }

    private void Finish(bool success)
    {
        if (_finished)
            return;
        _finished = true;
        _active = false;
        OnResult?.Invoke(success, _token);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var w = (float) Width;
        var h = (float) Height;

        handle.DrawRect(new UIBox2(0, 0, w, h), new Color(0.08f, 0.10f, 0.14f));

        var playW = w - Padding * 2 - Gap - ProgressBarWidth;
        var playX = Padding;
        var playY = Padding;
        var playH = h - Padding * 2;

        handle.DrawRect(new UIBox2(playX, playY, playX + playW, playY + playH),
            new Color(0.05f, 0.13f, 0.22f));

        var barCY = playY + _barPos * playH;
        var barHPx = BarHeightFrac / 2f * playH;
        handle.DrawRect(
            new UIBox2(playX, barCY - barHPx, playX + playW, barCY + barHPx),
            new Color(0.05f, 0.75f, 0.20f, 0.85f));

        var fishY = playY + _fishPos * playH;
        handle.DrawRect(
            new UIBox2(
                playX + playW / 2f - FishRadius,
                fishY - FishRadius,
                playX + playW / 2f + FishRadius,
                fishY + FishRadius),
            new Color(0.20f, 0.85f, 1f));

        handle.DrawRect(
            new UIBox2(playX, playY, playX + playW, playY + playH),
            new Color(0.30f, 0.40f, 0.50f, 0.6f),
            filled: false);

        var progX = playX + playW + Gap;
        handle.DrawRect(new UIBox2(progX, playY, progX + ProgressBarWidth, playY + playH),
            new Color(0.15f, 0.15f, 0.15f));

        var fillH = _progress * playH;
        handle.DrawRect(
            new UIBox2(progX, playY + playH - fillH, progX + ProgressBarWidth, playY + playH),
            new Color(0.95f, 0.80f, 0.10f));

        handle.DrawRect(
            new UIBox2(progX, playY, progX + ProgressBarWidth, playY + playH),
            new Color(0.40f, 0.40f, 0.40f, 0.6f),
            filled: false);
    }
}
