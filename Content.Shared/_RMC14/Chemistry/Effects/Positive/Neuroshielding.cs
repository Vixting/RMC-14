using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Neuroshielding : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Protects the brain from neurological damage caused by toxins.";
    }

    // TODO RMC14: liver organ damage

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryRemoveStatusEffect(args.TargetEntity, "Dazed");
        status.TryAddStatusEffectDuration(args.TargetEntity, ResistNeuroStatus, TimeSpan.FromSeconds(2));
    }

    // TODO RMC14: brain damage on critical overdose

    public static readonly EntProtoId ResistNeuroStatus = "RMCStatusEffectResistNeuro";
}
