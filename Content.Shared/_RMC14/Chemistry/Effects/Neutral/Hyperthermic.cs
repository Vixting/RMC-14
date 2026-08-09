using Content.Shared._RMC14.Stamina;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Stunnable;
using Content.Shared.Temperature;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hyperthermic : RMCChemicalEffect
{
    private static readonly float MaxTemperature = TemperatureHelpers.CelsiusToKelvin(120f);

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes an exothermic reaction when metabolized, increasing internal body temperature.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        var newTemp = MathF.Min(MaxTemperature, temperature.GetTemperature(args.TargetEntity) + (float) potency * 2f);
        temperature.ForceChangeTemperature(args.TargetEntity, newTemp);

        if ((float) potency >= 2f)
        {
            var dazed = args.EntityManager.System<RMCDazedSystem>();
            dazed.TryDaze(args.TargetEntity, TimeSpan.FromSeconds(1), false);

            var stamina = args.EntityManager.System<RMCStaminaSystem>();
            stamina.DoStaminaDamage(args.TargetEntity, (double) potency);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        temperature.ForceChangeTemperature(args.TargetEntity, temperature.GetTemperature(args.TargetEntity) + (float) potency * 5f);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds(2), true);
    }
}
