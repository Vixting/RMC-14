using Content.Client._RMC14.Particles;
using Content.Shared._RMC14.Mortar;
using Content.Shared._RMC14.Particles;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Mortar;

public sealed class MortarSystem : SharedMortarSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly ParticleSystem _particles = default!;

    private const string AnimationKey = "rmc_mortar_fire";

    private static readonly ProtoId<ParticleEffectPrototype> LaunchSmoke = "RMCMortarLaunchSmoke";
    private static readonly ProtoId<ParticleEffectPrototype> LaunchFlash = "RMCGauFlash";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<MortarFiredEvent>(OnMortarFiredEvent);
        SubscribeLocalEvent<MortarComponent, AfterAutoHandleStateEvent>(OnMortarHandleState);
    }

    private void OnMortarFiredEvent(MortarFiredEvent ev)
    {
        if (!TryGetEntity(ev.Mortar, out var mortarId) ||
            !TryComp(mortarId, out MortarComponent? mortar))
        {
            return;
        }

        _particles.CreateParticle(LaunchSmoke, mortarId.Value, attach: false);
        _particles.CreateParticle(LaunchFlash, mortarId.Value, attach: false);

        if (_animation.HasRunningAnimation(mortarId.Value, AnimationKey))
            return;

        _animation.Play(mortarId.Value,
            new Animation
            {
                Length = mortar.AnimationTime,
                AnimationTracks =
                {
                    new AnimationTrackSpriteFlick
                    {
                        LayerKey = mortar.AnimationLayer,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame(mortar.AnimationState, 0f),
                            new AnimationTrackSpriteFlick.KeyFrame(mortar.DeployedState, 0.3f),
                        },
                    },
                },
            },
            AnimationKey);
    }

    private void OnMortarHandleState(Entity<MortarComponent> mortar, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(mortar, out UserInterfaceComponent? ui))
                return;

            foreach (var open in ui.ClientOpenInterfaces.Values)
            {
                if (open is MortarBui bui)
                    bui.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(MortarBui)}:\n{e}");
        }
    }
}
