using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class DNADisintegrating : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> CellularType = "Cellular";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Immediately disintegrates the DNA of all organic cells it comes into contact with.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[CellularType] = potency * 10f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    // TODO RMC14: cm's real mechanic 5 stage disesa
    // close dmg >= 190 triggers disease
    // stage 2-4 flavour text, throat / skin discomfort, limb dmg, paralysis, hearing hive whipsers
    // stage 5 apply tox dmg, 40%/tick to gib body part & become T1 xeno
}
