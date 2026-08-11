using Content.Shared.CCVar;
using Content.Shared._RMC14.Drugs;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Drugs;

public sealed class RMCRainbowOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Rainbow";

    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    private readonly SharedStatusEffectsSystem _statusEffects = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _rainbowShader;

    public float Intoxication = 0.0f;
    public float Phase = 0.0f;

    private const float FadeInSeconds = 1.0f;
    private const float FadeOutSeconds = 1.5f;
    private float _timeScale = 0.0f;
    private float _warpScale = 0.0f;

    private float EffectScale => Math.Clamp(Intoxication, 0.0f, 1.0f);

    public RMCRainbowOverlay()
    {
        IoCManager.InjectDependencies(this);

        _statusEffects = _sysMan.GetEntitySystem<SharedStatusEffectsSystem>();

        _rainbowShader = _prototypeManager.Index(Shader).InstanceUnique();
        _config.OnValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged, invokeImmediately: true);
    }

    private void OnReducedMotionChanged(bool reducedMotion)
    {
        _timeScale = reducedMotion ? 0.0f : 1.0f;
        _warpScale = reducedMotion ? 0.0f : 1.0f;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null)
            return;

        var active = _statusEffects.TryGetEffectsEndTimeWithComp<RMCSeeingRainbowsStatusEffectComponent>(playerEntity, out _);

        if (active)
            Intoxication = MathF.Min(Intoxication + args.DeltaSeconds / FadeInSeconds, 1.0f);
        else
            Intoxication = MathF.Max(Intoxication - args.DeltaSeconds / FadeOutSeconds, 0.0f);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return EffectScale > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _rainbowShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _rainbowShader.SetParameter("colorScale", EffectScale);
        _rainbowShader.SetParameter("timeScale", _timeScale);
        _rainbowShader.SetParameter("warpScale", _warpScale * EffectScale);
        _rainbowShader.SetParameter("phase", Phase);
        handle.UseShader(_rainbowShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
