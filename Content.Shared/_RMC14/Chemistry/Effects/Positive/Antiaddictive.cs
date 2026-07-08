using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Antiaddictive : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "StatusEffectSeeingRainbow";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Stops all bodily cravings towards addictive chemical substances.";
    }

    // TODO RMC14: mob effect - cures addiction

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddTime(args.TargetEntity, SeeingRainbows, TimeSpan.FromSeconds((float) potency));
    }
}
