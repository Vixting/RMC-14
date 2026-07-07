using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Antispasmodic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Relaxes smooth muscles and treats muscle spasms. High concentrations can cause respiratory failure and cardiac arrest.";
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.25f))
        {
            var stun = args.EntityManager.System<SharedStunSystem>();
            stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds((float) potency), true);
        }

        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = potency * 0.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        // TODO RMC14: mob effect - heart organ damage
    }
}
