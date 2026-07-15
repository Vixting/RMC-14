using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hypothermic : RMCChemicalEffect
{
    private static readonly ProtoId<ReagentPrototype> Cryoxadone = "CMCryoxadone";
    private static readonly ProtoId<ReagentPrototype> Clonexadone = "CMClonexadone";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes an endothermic reaction when metabolized, decreasing internal body temperature.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        temperature.ForceChangeTemperature(args.TargetEntity, temperature.GetTemperature(args.TargetEntity) - (float) potency * 2f);

        if (temperature.GetTemperature(args.TargetEntity) >= Atmospherics.T0C)
            return;

        if (args.Source is not { } source ||
            (!source.ContainsPrototype(Cryoxadone) && !source.ContainsPrototype(Clonexadone)))
        {
            return;
        }

        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream) &&
            bloodstream.BleedAmount > 0)
        {
            var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();
            bloodstreamSystem.TryModifyBleedAmount((args.TargetEntity, bloodstream), -bloodstream.BleedAmount);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        temperature.ForceChangeTemperature(args.TargetEntity, temperature.GetTemperature(args.TargetEntity) - (float) potency * 5f);

        var status = args.EntityManager.System<StatusEffectsSystem>();
        status.TryAddStatusEffect<RMCBlindedComponent>(args.TargetEntity, "Blinded", TimeSpan.FromSeconds(6), true);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds(2), true);
    }
}
