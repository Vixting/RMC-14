using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Jittering;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Cardiostabilizing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Stabilizes the cardiac cycle when under shock.";
    }

    // TODO RMC14: mob effect - pain reduction, heart organ damage on critical overdose
    // TODO RMC14: lose breath thing

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var jitter = args.EntityManager.System<SharedJitteringSystem>();
        jitter.DoJitter(args.TargetEntity, TimeSpan.FromSeconds(2), true);

        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds(2), true);
    }
}
