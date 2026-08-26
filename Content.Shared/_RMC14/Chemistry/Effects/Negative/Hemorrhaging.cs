using Content.Shared._RMC14.Chemistry.Buildup;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Botany.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hemorrhaging : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private const float TouchBleedMultiplier = 0.25f;
    private const float HealingReductionDissipation = 0.4f;
    private const float HealingReductionMax = 50f;

    private static readonly ProtoId<EmotePrototype> CoughEmote = "Cough";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes hemorrhaging.";
    }

    protected override void TickHydroTray(PlantHolderComponent plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = 0.4f * Potency * (float) args.Quantity;
        plant.Health -= amount;
        plant.MutationMod += amount;
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
        {
            TouchReaction(args);
            return;
        }

        if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
            return;

        var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();
        bloodstreamSystem.TryModifyBleedAmount((args.TargetEntity, bloodstream), (float) potency);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(MathF.Min(1f, (float) potency * 0.05f)))
        {
            var emoteSystem = args.EntityManager.System<SharedRMCEmoteSystem>();
            emoteSystem.TryEmoteWithChat(
                args.TargetEntity,
                CoughEmote,
                hideLog: true,
                ignoreActionBlocker: true,
                forceEmote: true
            );
        }
    }

    private void TouchReaction(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var isXenoHuman = entities.HasComponent<XenoComponent>(target) ||
            (entities.HasComponent<HumanoidAppearanceComponent>(target) && !entities.HasComponent<SynthComponent>(target));
        if (!isXenoHuman)
            return;

        var volume = (float) args.Quantity;
        var magnitude = (float) ActualPotency * volume * TouchBleedMultiplier;
        var buildup = entities.System<RMCHealingReductionSystem>();
        buildup.AddBuildup(target, magnitude, HealingReductionDissipation, HealingReductionMax);
    }

    // TODO RMC14: organ damage on overdose

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
            return;

        var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();
        bloodstreamSystem.TryModifyBleedAmount((args.TargetEntity, bloodstream), (float) potency * 4f);
    }
}
