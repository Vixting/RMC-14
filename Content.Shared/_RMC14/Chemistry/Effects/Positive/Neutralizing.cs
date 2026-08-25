using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Neutralizing : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    private const float PlasmaDrainMultiplier = 5f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Neutralizes certain reactive chemicals and plasmas on contact. Unsafe to administer intravenously.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
        {
            TouchReaction(args);
            return;
        }

        var damage = new DamageSpecifier();
        damage.DamageDict[HeatType] = potency;
        damage.DamageDict[PoisonType] = potency * 0.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    private void TouchReaction(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (entities.HasComponent<MobStateComponent>(target))
        {
            if (entities.TryGetComponent<FlammableComponent>(target, out var flammable))
            {
                var flammableSystem = entities.System<SharedRMCFlammableSystem>();
                flammableSystem.Extinguish((target, flammable));
            }

            if (entities.TryGetComponent<XenoPlasmaComponent>(target, out var plasma))
            {
                var plasmaSystem = entities.System<XenoPlasmaSystem>();
                var drain = FixedPoint2.New(PlasmaDrainMultiplier * (float) args.Quantity * ActualPotency);
                plasmaSystem.RemovePlasma((target, plasma), drain);
            }

            return;
        }

        // TODO RMC14: neutralize turf/tile acid pools
        if (entities.HasComponent<TileFireComponent>(target))
            entities.QueueDeleteEntity(target);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[HeatType] = potency * 2f;
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    // TODO RMC14: liver damage on critical overdose
}
