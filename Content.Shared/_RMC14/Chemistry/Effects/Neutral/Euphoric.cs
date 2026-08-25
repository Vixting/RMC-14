using Content.Shared._RMC14.Emote;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Euphoric : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    private static readonly ProtoId<EmotePrototype>[] Emotes =
    [
        "Laugh",
        "RMCGiggle",
        "RMCGrin",
        "RMCSmile",
        "RMCTwitch",
    ];

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes the release of endorphins, resulting in intense excitement and happiness.";
    }

    // TODO RMC14: pain reduction

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.05f * (float) potency))
            return;

        var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
        emote.TryEmoteWithChat(args.TargetEntity, random.Pick(Emotes), hideLog: true, ignoreActionBlocker: true, forceEmote: true);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.05f * (float) potency))
            return;

        var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
        emote.TryEmoteWithChat(args.TargetEntity, "Laugh", hideLog: true, ignoreActionBlocker: true, forceEmote: true);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = potency * 5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
