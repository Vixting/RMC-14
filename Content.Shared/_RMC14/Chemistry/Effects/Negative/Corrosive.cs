using Content.Shared._RMC14.Chemistry.Buildup;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Botany.Components;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Corrosive : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> CausticType = "Caustic";
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Deals [color=red]{PotencyPerSecond}[/color] burn damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 2}[/color] burn damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] burn damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
        {
            HandleTouch(damageable, args);
            return;
        }

        var damage = new DamageSpecifier();
        damage.DamageDict[CausticType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    private void HandleTouch(DamageableSystem damageable, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (!entities.HasComponent<MobStateComponent>(target))
        {
            HandleTouchItem(args);
            return;
        }

        // meltprob = potency * POTENCY_MULTIPLIER_HIGH (3), expressed as a 0-1 chance here
        var meltChance = (float) ActualPotency * 0.03f;
        var isHuman = entities.HasComponent<HumanoidAppearanceComponent>(target) && !entities.HasComponent<SynthComponent>(target);
        if (isHuman)
        {
            var inventory = entities.System<InventorySystem>();
            var popup = entities.System<SharedPopupSystem>();
            var random = IoCManager.Resolve<IRobustRandom>();

            if (inventory.TryGetSlotEntity(target, "head", out var head))
            {
                if (random.Prob(meltChance) && !entities.HasComponent<RMCUnacidableComponent>(head))
                {
                    inventory.TryUnequip(target, "head", force: true);
                    entities.QueueDeleteEntity(head);
                    popup.PopupEntity("Your headgear melts away but protects you from the acid!", target, target, PopupType.MediumCaution);
                }
                else
                {
                    popup.PopupEntity("Your headgear protects you from the acid.", target, target, PopupType.Small);
                }

                return;
            }

            if (inventory.TryGetSlotEntity(target, "mask", out var mask))
            {
                if (random.Prob(meltChance) && !entities.HasComponent<RMCUnacidableComponent>(mask))
                {
                    inventory.TryUnequip(target, "mask", force: true);
                    entities.QueueDeleteEntity(mask);
                    popup.PopupEntity("Your mask melts away but protects you from the acid!", target, target, PopupType.MediumCaution);
                }
                else
                {
                    popup.PopupEntity("Your mask protects you from the acid.", target, target, PopupType.Small);
                }

                return;
            }

            // cm13's glasses branch has no "protects you" message on a failed/blocked roll, unlike head/mask
            if (inventory.TryGetSlotEntity(target, "eyes", out var eyes))
            {
                if (random.Prob(meltChance) && !entities.HasComponent<RMCUnacidableComponent>(eyes))
                {
                    inventory.TryUnequip(target, "eyes", force: true);
                    entities.QueueDeleteEntity(eyes);
                    popup.PopupEntity("Your eyewear melts away!", target, target, PopupType.MediumCaution);
                }

                return;
            }
        }

        // TODO RMC14: head burn damage instead of generic brute
        var volume = (float) args.Quantity;
        var rmcDamageable = entities.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeDamageCached(target, BruteGroup, MathF.Min(6f, volume));
        damageable.TryChangeDamage(target, damage, true, interruptsDoAfters: false);

        if (entities.HasComponent<XenoComponent>(target) && ActualPotency > 2f)
        {
            var toxicBuildup = entities.System<RMCToxicBuildupSystem>();
            var magnitude = ActualPotency * volume * 0.25f;
            toxicBuildup.AddBuildup(target, magnitude, 1f / 3f, 75f);
        }
    }

    private void HandleTouchItem(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (!entities.HasComponent<ItemComponent>(target))
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob((float) ActualPotency * 0.1f))
            entities.QueueDeleteEntity(target);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[CausticType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[CausticType] = potency * 5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = Potency * (float) args.Quantity;
        if (plant.WeedLevel > 0)
            plant.WeedLevel = MathF.Max(0f, plant.WeedLevel - amount);

        if (plant.PestLevel > 0)
            plant.PestLevel = MathF.Max(0f, plant.PestLevel - amount);
    }
}
