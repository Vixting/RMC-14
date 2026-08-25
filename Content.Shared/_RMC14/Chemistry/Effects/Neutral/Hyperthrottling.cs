using Content.Shared._RMC14.Language.Systems;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Synth;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hyperthrottling : RMCChemicalEffect
{
    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "RMCStatusEffectSeeingRainbow";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes the brain to operate at several thousand times its normal speed, allowing understanding of all spoken languages.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity) ||
            args.EntityManager.HasComponent<SynthComponent>(args.TargetEntity))
        {
            return;
        }

        var penalty = 1f - (float) PotencyPerSecond * 0.05f;
        var speed = args.EntityManager.System<TemporarySpeedModifiersSystem>();
        var modifiers = new List<TemporarySpeedModifierSet> { new(TimeSpan.FromSeconds(2), penalty, penalty) };
        speed.ModifySpeed(args.TargetEntity, modifiers);

        var understanding = args.EntityManager.System<RMCUniversalUnderstandingSystem>();
        understanding.GrantFor(args.TargetEntity, TimeSpan.FromSeconds(2));

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var cap = TimeSpan.FromSeconds(10d);
        var remaining = TimeSpan.Zero;
        if (status.TryGetTime(args.TargetEntity, SeeingRainbows, out var time) && time.EndEffectTime is { } end)
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            remaining = end - timing.CurTime;
        }

        var toAdd = TimeSpan.FromSeconds((double) ActualPotency * 0.5d);
        if (remaining + toAdd > cap)
            toAdd = cap - remaining;

        if (toAdd > TimeSpan.Zero)
            status.TryAddStatusEffectDuration(args.TargetEntity, SeeingRainbows, toAdd);
    }

    // TODO RMC14: brain damage on overdose

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds((float) potency * 2f), true);
    }
}
