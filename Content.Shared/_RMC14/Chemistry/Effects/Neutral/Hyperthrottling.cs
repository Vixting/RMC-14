using Content.Shared._RMC14.Language.Systems;
using Content.Shared._RMC14.Movement;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hyperthrottling : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes the brain to operate at several thousand times its normal speed, allowing understanding of all spoken languages.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var penalty = 1f - (float) PotencyPerSecond * 0.05f;
        var speed = args.EntityManager.System<TemporarySpeedModifiersSystem>();
        var modifiers = new List<TemporarySpeedModifierSet> { new(TimeSpan.FromSeconds(2), penalty, penalty) };
        speed.ModifySpeed(args.TargetEntity, modifiers);

        var understanding = args.EntityManager.System<RMCUniversalUnderstandingSystem>();
        understanding.GrantFor(args.TargetEntity, TimeSpan.FromSeconds(2));
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 3f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds((float) potency * 2f), true);
    }
}
