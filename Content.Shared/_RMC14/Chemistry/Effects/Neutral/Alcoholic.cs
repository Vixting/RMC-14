using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Drunk;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Alcoholic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Slows brain function response to stimuli, causing intoxication.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var drunk = args.EntityManager.System<SharedDrunkSystem>();
        drunk.TryApplyDrunkenness(args.TargetEntity, (float) potency * 2f);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var drunk = args.EntityManager.System<SharedDrunkSystem>();
        drunk.TryApplyDrunkenness(args.TargetEntity, (float) potency * 4f);

        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 0.25f;
        damage.DamageDict[AsphyxiationType] = potency * 0.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.1f))
        {
            var vomit = args.EntityManager.System<RMCVomitSystem>();
            vomit.StartVomit(args.TargetEntity);
        }
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var drunk = args.EntityManager.System<SharedDrunkSystem>();
        drunk.TryApplyDrunkenness(args.TargetEntity, (float) potency * 8f);

        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 0.5f;
        damage.DamageDict[AsphyxiationType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        // TODO RMC14: mob effect - liver organ damage
    }
}
