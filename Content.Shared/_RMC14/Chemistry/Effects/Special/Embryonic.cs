using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Embryonic : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Carries an infectious parasitic embryonic organism.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Source == null || args.Reagent == null)
            return;

        if (!CanBeInfected(args.EntityManager, args.TargetEntity))
        {
            var quantity = args.Source.GetTotalPrototypeQuantity(args.Reagent.ID);
            args.Source.RemoveReagent(args.Reagent.ID, quantity);
            return;
        }

        args.EntityManager.EnsureComponent<VictimInfectedComponent>(args.TargetEntity);

        var popup = args.EntityManager.System<SharedPopupSystem>();
        popup.PopupEntity("Your stomach cramps and you suddenly feel very sick!", args.TargetEntity, args.TargetEntity, PopupType.MediumCaution);

        var consumed = args.Source.GetTotalPrototypeQuantity(args.Reagent.ID);
        args.Source.RemoveReagent(args.Reagent.ID, consumed);
    }

    private static bool CanBeInfected(IEntityManager entityManager, EntityUid target)
    {
        return !entityManager.HasComponent<VictimInfectedComponent>(target) &&
               !entityManager.HasComponent<SynthComponent>(target) &&
               entityManager.HasComponent<InfectableComponent>(target);
    }
}
