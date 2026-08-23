using Content.Shared._RMC14.Synth;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Fluxing : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Liquefies large crystalline and metallic structures in the body, allowing them to be excreted through the skin.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(MathF.Min(0.05f * (float) ActualPotency, 1f)))
            return;

        if (args.EntityManager.HasComponent<SynthComponent>(args.TargetEntity))
        {
            var damage = new DamageSpecifier();
            damage.DamageDict[BluntType] = potency;
            damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
            return;
        }

        if (args.EntityManager.TryGetComponent<EmbeddedContainerComponent>(args.TargetEntity, out var container) &&
            container.EmbeddedObjects.Count > 0)
        {
            var embedded = random.Pick(container.EmbeddedObjects);
            args.EntityManager.System<SharedProjectileSystem>().EmbedDetach(embedded, null);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency * 2f;
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency * 2f;
        damage.DamageDict[PoisonType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
