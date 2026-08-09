using Content.Shared._RMC14.Chemistry.Effects;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Effects;

/// <summary>
/// Single dispatch system for all <see cref="RMCChemicalEffect"/>s. They raise <see cref="UntypedEntityEffectEvent"/>
/// through the new effect pipeline; this system reconstructs the reagent context set by the driver on
/// <see cref="ActiveReagentEffectComponent"/> and invokes <see cref="RMCChemicalEffect.Apply"/>.
/// </summary>
public sealed class RMCChemicalEffectSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UntypedEntityEffectEvent>(OnEffect);
    }

    private void OnEffect(ref UntypedEntityEffectEvent args)
    {
        if (args.Effect is not RMCEntityEffect effect)
            return;

        var target = args.Target;

        EntityEffectReagentArgs reagentArgs;
        if (TryComp(target, out ActiveReagentEffectComponent? ctx))
        {
            Content.Shared.Chemistry.Reagent.ReagentPrototype? reagent = null;
            if (ctx.Reagent is { } reagentId)
                _prototype.TryIndex(reagentId, out reagent);

            reagentArgs = new EntityEffectReagentArgs(
                target,
                EntityManager,
                ctx.OrganEntity,
                ctx.Source,
                ctx.Quantity,
                reagent,
                ctx.Method,
                args.Scale);
        }
        else
        {
            reagentArgs = new EntityEffectReagentArgs(target, EntityManager, null, null, 0, null, null, args.Scale);
        }

        effect.Effect(reagentArgs);
    }
}
