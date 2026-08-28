using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Psychostimulating : RMCChemicalEffect
{
    private static readonly EntProtoId ConfusedStatus = "RMCStatusEffectConfused";
    private static readonly TimeSpan ReductionDelay = TimeSpan.FromMinutes(5);

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Stimulates psychological functions, causing increased awareness, focus, and anti-depressing effects.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var timing = IoCManager.Resolve<IGameTiming>();
        var throttle = entities.EnsureComponent<PsychostimulatingThrottleComponent>(args.TargetEntity);
        if (timing.CurTime < throttle.NextReduction)
            return;

        throttle.NextReduction = timing.CurTime + ReductionDelay;

        var status = entities.System<SharedStatusEffectsSystem>();
        var actualPotency = (float) ActualPotency;
        if (actualPotency >= 4f)
        {
            status.TryRemoveStatusEffect(args.TargetEntity, ConfusedStatus);
            return;
        }

        var step = actualPotency >= 3f ? 3f : actualPotency >= 2f ? 2f : 1f;
        status.TryAddTime(args.TargetEntity, ConfusedStatus, TimeSpan.FromSeconds(-step));
    }

    // TODO RMC14: brain damage on overdose
    // TODO RMC14: brain damage on critical overdose
}

[RegisterComponent]
public sealed partial class PsychostimulatingThrottleComponent : Component
{
    [DataField]
    public TimeSpan NextReduction;
}
