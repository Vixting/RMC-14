using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Drunk;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Alcoholic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    private const string ConfusedStatus = "RMCStatusEffectConfused";
    private const string DrowsinessStatus = "StatusEffectDrowsiness";
    private const string ForcedSleepingStatus = "StatusEffectForcedSleeping";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Slows brain function response to stimuli, causing intoxication.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        if (status.HasStatusEffect(args.TargetEntity, Neuroshielding.ResistNeuroStatus))
            return;

        var drunk = args.EntityManager.System<SharedDrunkSystem>();
        drunk.TryApplyDrunkenness(args.TargetEntity, (float) potency * 2f);

        var timing = IoCManager.Resolve<IGameTiming>();
        var actualPotency = (float) ActualPotency;

        ApplyCapped(status, timing, args.TargetEntity, DrowsinessStatus, 0.5f * actualPotency, 5f * actualPotency);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.5f) || actualPotency >= 5f)
            ApplyCapped(status, timing, args.TargetEntity, ConfusedStatus, 0.5f * actualPotency, 5f * actualPotency);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var drunk = args.EntityManager.System<SharedDrunkSystem>();
        drunk.TryApplyDrunkenness(args.TargetEntity, (float) potency * 4f);

        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 0.5f;
        damage.DamageDict[AsphyxiationType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var random = IoCManager.Resolve<IRobustRandom>();
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();
        var actualPotency = (float) ActualPotency;

        ApplyCapped(status, timing, args.TargetEntity, DrowsinessStatus, actualPotency, 7.5f * actualPotency);

        if (random.Prob(0.02f))
            ApplyCapped(status, timing, args.TargetEntity, ForcedSleepingStatus, 0.5f * actualPotency, 7.5f * actualPotency);

        if (random.Prob(actualPotency * 0.02f))
        {
            var vomit = args.EntityManager.System<RMCVomitSystem>();
            vomit.StartVomit(args.TargetEntity);
        }

        if (random.Prob(0.75f) || actualPotency >= 5f)
            ApplyCapped(status, timing, args.TargetEntity, ConfusedStatus, actualPotency, 7.5f * actualPotency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var drunk = args.EntityManager.System<SharedDrunkSystem>();
        drunk.TryApplyDrunkenness(args.TargetEntity, (float) potency * 8f);

        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damage.DamageDict[AsphyxiationType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();
        var random = IoCManager.Resolve<IRobustRandom>();
        var actualPotency = (float) ActualPotency;

        ApplyCapped(status, timing, args.TargetEntity, ConfusedStatus, 2f * actualPotency, 10f * actualPotency);
        ApplyCapped(status, timing, args.TargetEntity, DrowsinessStatus, 2f * actualPotency, 10f * actualPotency);

        var sleepChance = Math.Clamp(5f * actualPotency * 0.01f, 0f, 1f);
        if (random.Prob(sleepChance))
            ApplyCapped(status, timing, args.TargetEntity, ForcedSleepingStatus, 0.5f * actualPotency, 10f * actualPotency);

        var vomitChance = Math.Clamp(5f * actualPotency * 0.01f, 0f, 1f);
        if (random.Prob(vomitChance))
        {
            var vomit = args.EntityManager.System<RMCVomitSystem>();
            vomit.StartVomit(args.TargetEntity);
        }

        // TODO RMC14: liver damage
    }

    private static void ApplyCapped(
        SharedStatusEffectsSystem status,
        IGameTiming timing,
        EntityUid target,
        string proto,
        float increment,
        float cap)
    {
        if (cap <= 0f)
            return;

        var current = 0f;
        if (status.TryGetTime(target, proto, out var time) && time.EndEffectTime is { } end)
        {
            var remaining = (float) (end - timing.CurTime).TotalSeconds;
            if (remaining > 0f)
                current = remaining;
        }

        var next = Math.Min(current + increment, cap);
        if (next <= 0f)
            return;

        status.TrySetStatusEffectDuration(target, proto, TimeSpan.FromSeconds(next));
    }
}
