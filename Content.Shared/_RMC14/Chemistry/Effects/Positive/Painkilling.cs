using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Painkilling : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "RMCStatusEffectSeeingRainbow";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Binds to opioid receptors in the brain and spinal cord, reducing pain.";
    }

    // TODO RMC14: mob effect - pain reduction

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddStatusEffectDuration(args.TargetEntity, SeeingRainbows, TimeSpan.FromSeconds((float) potency * 0.5f));

        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 2f;
        damage.DamageDict[AsphyxiationType] = 1.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        // TODO RMC14: mob effect - liver organ damage
    }
}
