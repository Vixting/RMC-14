using Content.Shared._RMC14.Emote;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hypnotic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";
    private static readonly ProtoId<EmotePrototype> YawnEmote = "Yawn";
    private const float ConfusedThresholdSeconds = 25f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes the body to release melatonin, resulting in increased sleepiness.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;
        var status = entities.System<SharedStatusEffectsSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();

        var remaining = TimeSpan.Zero;
        if (status.TryGetTime(target, "RMCStatusEffectConfused", out var time) && time.EndEffectTime is { } end)
            remaining = end - timing.CurTime;

        var delta = TimeSpan.FromSeconds(2f * (float) potency);

        if (remaining.TotalSeconds < ConfusedThresholdSeconds)
        {
            status.TryAddStatusEffectDuration(target, "RMCStatusEffectConfused", delta);

            var random = IoCManager.Resolve<IRobustRandom>();
            if (random.Prob(0.25f))
            {
                var emote = entities.System<SharedRMCEmoteSystem>();
                emote.TryEmoteWithChat(target, YawnEmote, hideLog: true, ignoreActionBlocker: true, forceEmote: true);
            }
        }
        else
        {
            status.TryAddStatusEffectDuration(target, "StatusEffectForcedSleeping", delta);
            status.TryAddTime(target, "RMCStatusEffectConfused", -delta);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds((float) potency), true);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = potency * 5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
