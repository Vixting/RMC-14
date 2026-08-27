using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Botany.Components;
using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Movement;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Hemogenic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var baseText = $"Restores [color=green]{PotencyPerSecond}[/color]cl of blood while not hungry.\n" +
                       $"Causes [color=red]{PotencyPerSecond}[/color] nutrient loss per second.\n" +
                       $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
                       $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] additional nutrient loss";

        return ActualPotency > 3
            ? $"Deals [color=red]{PotencyPerSecond}[/color] brute, [color=red]{PotencyPerSecond * 2}[/color] airloss damage, and slows you down.\n{baseText}"
            : baseText;
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!plant.Sampled)
            return;
        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.6f))
        {
            plant.Sampled = false;
            var popup = args.EntityManager.System<SharedPopupSystem>();
            popup.PopupEntity(Loc.GetString("plant-hemogenic-healed"), args.TargetEntity);
        }
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var hungerSystem = entityManager.System<HungerSystem>();

        if (!entityManager.TryGetComponent<HungerComponent>(target, out var hungerComponent) ||
            hungerSystem.GetHunger(hungerComponent) < 200)
            return;

        hungerSystem.ModifyHunger(target, -(float) potency);

        if (entityManager.TryGetComponent<BloodstreamComponent>(target, out var bloodstream))
        {
            var bloodstreamSystem = entityManager.System<SharedBloodstreamSystem>();
            bloodstreamSystem.TryModifyBloodLevel((target, bloodstream), potency);
        }

        var rmcBloodstreamSystem = entityManager.System<SharedRMCBloodstreamSystem>();
        var shouldApplyDamage = ActualPotency > 3 &&
                                rmcBloodstreamSystem.TryGetBloodSolution(target, out var bloodSolution) &&
                                bloodSolution.Volume > bloodSolution.MaxVolume + 10;
        if (!shouldApplyDamage)
            return;
        var rmcDamageable = entityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BruteGroup, potency);
        damage.DamageDict[AsphyxiationType] = potency * 2;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var penalty = MathF.Max(1f - (float) potency * 0.05f, 0.1f);
        var speed = entityManager.System<TemporarySpeedModifiersSystem>();
        var modifiers = new List<TemporarySpeedModifierSet> { new(TimeSpan.FromSeconds(2), penalty, penalty) };
        speed.ModifySpeed(target, modifiers);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var hungerSystem = entityManager.System<HungerSystem>();

        hungerSystem.ModifyHunger(target, -(float) potency * 5f);
    }
}
