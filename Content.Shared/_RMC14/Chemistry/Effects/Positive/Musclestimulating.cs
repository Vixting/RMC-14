using Content.Shared._RMC14.Chemistry.Buildup;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Musclestimulating : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private const float NutritionDrain = 0.5f * 0.05f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Stimulates neuromuscular junctions, increasing muscle contraction force and carry weight. High doses may exhaust the cardiac muscles.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
        {
            HandleTouch(args);
            return;
        }

        var boost = 1f + (float) PotencyPerSecond * 0.05f;
        var speed = args.EntityManager.System<TemporarySpeedModifiersSystem>();
        var modifiers = new List<TemporarySpeedModifierSet> { new(TimeSpan.FromSeconds(2), boost, boost) };
        speed.ModifySpeed(args.TargetEntity, modifiers);

        args.EntityManager.System<HungerSystem>().ModifyHunger(args.TargetEntity, -NutritionDrain);
    }

    private void HandleTouch(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var isXenoHuman = entities.HasComponent<XenoComponent>(target) ||
            (entities.HasComponent<HumanoidAppearanceComponent>(target) && !entities.HasComponent<SynthComponent>(target));
        if (!isXenoHuman)
            return;

        var potency = (float) ActualPotency;
        if (potency <= 0f)
            return;

        var volume = (float) args.Quantity;
        var buildup = entities.System<RMCSpeedBuildupSystem>();
        buildup.AddBuildup(target, volume, 1f / potency, potency * volume, increaseSpeed: true);
    }

    // TODO RMC14: heart organ damage on overdose

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BruteGroup, potency);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
