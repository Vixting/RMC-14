using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Neuroinhibiting : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Inhibits neurological processes in the brain, which can result in sight, hearing, and speech impairments. Restoration will require surgery.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var p = (float) potency;
        var target = args.TargetEntity;
        var entities = args.EntityManager;

        if (entities.System<SharedStatusEffectsSystem>().HasStatusEffect(target, Neuroshielding.ResistNeuroStatus))
            return;

        if (p > 0.5f)
            entities.EnsureComponent<RMCBlindDisabilityComponent>(target);
        else
            entities.EnsureComponent<RMCNearsightedComponent>(target);

        if (p > 1f)
            entities.EnsureComponent<RMCDeafDisabilityComponent>(target);

        if (p > 1.5f)
            entities.EnsureComponent<RMCMuteDisabilityComponent>(target);
    }

    // TODO RMC14: brain damage on overdose

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        args.EntityManager.EnsureComponent<RMCNervousComponent>(args.TargetEntity);
    }

    // TODO RMC14: mob effect - brain damage on critical overdose (no Brain damage type/organ system)
}
