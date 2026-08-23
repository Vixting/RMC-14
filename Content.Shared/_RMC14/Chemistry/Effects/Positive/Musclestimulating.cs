using Content.Shared._RMC14.Movement;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Musclestimulating : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Stimulates neuromuscular junctions, increasing muscle contraction force and carry weight. High doses may exhaust the cardiac muscles.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var boost = 1f + (float) PotencyPerSecond * 0.05f;
        var speed = args.EntityManager.System<TemporarySpeedModifiersSystem>();
        var modifiers = new List<TemporarySpeedModifierSet> { new(TimeSpan.FromSeconds(2), boost, boost) };
        speed.ModifySpeed(args.TargetEntity, modifiers);
    }

    // TODO RMC14: speed buff on touch for xenos/marines

    // TODO RMC14: heart organ damage on overdose

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
