using Content.Shared._RMC14.Chemistry.Buildup;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Disrupting : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Disrupts neurological processes related to communication in animals.";
    }

    // TODO RMC14: brain damage overdose

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
            HandleTouch(args);
    }

    private void HandleTouch(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;
        if (!entities.HasComponent<XenoComponent>(target))
            return;

        var volume = (float) args.Quantity;
        var magnitude = volume * (float) ActualPotency * 1.2f;
        var interference = entities.System<RMCHivemindInterferenceSystem>();
        interference.AddBuildup(target, magnitude, 1f, 90f);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds((float) potency * 0.1), true);
    }
}
