using Content.Shared._RMC14.Emote;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hallucinogenic : RMCChemicalEffect
{
    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "RMCStatusEffectSeeingRainbow";

    private static readonly EntProtoId ConfusedStatus = "RMCStatusEffectConfused";

    private static readonly ProtoId<EmotePrototype>[] Emotes =
    [
        "RMCTwitch",
        "RMCDrool",
        "RMCGiggle",
    ];

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes perception-like experiences without an external stimulus, such as hallucinations.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddStatusEffectDuration(args.TargetEntity, SeeingRainbows, TimeSpan.FromSeconds((float) potency));

        if ((float) potency > 1f)
        {
            var jitter = args.EntityManager.System<SharedJitteringSystem>();
            jitter.DoJitter(args.TargetEntity, TimeSpan.FromSeconds(2), false);
        }

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.05f))
        {
            var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
            emote.TryEmoteWithChat(args.TargetEntity, random.Pick(Emotes), hideLog: true, ignoreActionBlocker: true, forceEmote: true);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddStatusEffectDuration(args.TargetEntity, SeeingRainbows, TimeSpan.FromSeconds(10));

        var jitter = args.EntityManager.System<SharedJitteringSystem>();
        jitter.DoJitter(args.TargetEntity, TimeSpan.FromSeconds(2), false);

        status.TrySetStatusEffectDuration(args.TargetEntity, ConfusedStatus, TimeSpan.FromSeconds(3));
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14: brain damage

        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds(2), true);
    }
}
