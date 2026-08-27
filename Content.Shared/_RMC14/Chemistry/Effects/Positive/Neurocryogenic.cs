using Content.Shared._RMC14.Chemistry.Buildup;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Temperature;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Neurocryogenic : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    // I hope this is actaully used somewhere
    private const float CryoLiquidThreshold = 210f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes a temporal freeze of neurological processes in the brain, preserving it for long periods of time.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
        {
            HandleTouch(args);
            return;
        }

        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (entities.TryGetComponent(target, out MobStateComponent? mobStateComp) &&
            entities.System<MobStateSystem>().IsDead(target, mobStateComp))
        {
            var rotting = entities.System<SharedRottingSystem>();
            rotting.ReduceAccumulator(target, TimeSpan.FromSeconds((double) potency * 5));
            return;
        }

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.1f))
            entities.System<SharedPopupSystem>().PopupEntity("You feel like you have the worst brain freeze ever!", target, target);

        var stun = entities.System<SharedStunSystem>();
        stun.TryKnockdown(target, TimeSpan.FromSeconds(2), true);
        stun.TryStun(target, TimeSpan.FromSeconds(2), true);
    }

    private void HandleTouch(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var isXenoHuman = entities.HasComponent<XenoComponent>(target) ||
            (entities.HasComponent<HumanoidAppearanceComponent>(target) && !entities.HasComponent<SynthComponent>(target));
        if (!isXenoHuman)
            return;

        var volume = (float) args.Quantity;
        var magnitude = (float) ActualPotency * volume * 0.5f;
        var buildup = entities.System<RMCSpeedBuildupSystem>();
        buildup.AddBuildup(target, magnitude, dissipationRate: 0.4f, maxBuildup: 10f, increaseSpeed: false);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        var newTemperature = MathF.Max(CryoLiquidThreshold, temperature.GetTemperature(args.TargetEntity) - (float) potency * 5f);
        temperature.ForceChangeTemperature(args.TargetEntity, newTemperature);
    }

    // TODO RMC14: critical brain damage
}
