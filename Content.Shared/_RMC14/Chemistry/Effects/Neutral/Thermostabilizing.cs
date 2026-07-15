using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Temperature;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Thermostabilizing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Stabilizes the temperature of the body to [color=green]{TemperatureHelpers.CelsiusToKelvin(Atmospherics.NormalBodyTemperature)}[/color] kelvins, by [color=green]{30f * PotencyPerSecond}[/color] K at a time.\n" +
               $"Overdoses cause a [color=red]2[/color] second knockdown.\n" +
               $"Critical overdoses cause drowsiness.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var sys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedRMCTemperatureSystem>();
        var current = sys.GetTemperature(args.TargetEntity);
        var normalBodyTemp = TemperatureHelpers.CelsiusToKelvin(Atmospherics.NormalBodyTemperature);
        if (Math.Abs(current - normalBodyTemp) < 0.01)
            return;

        // cmss13's prop_neutral.dm: 20*potency*delta_time*TEMPERATURE_DAMAGE_COEFFICIENT(1.5) = 30*potency.
        var change = 30f * potency.Float();

        var temp = current > normalBodyTemp
            ? Math.Max(normalBodyTemp, current - change)
            : Math.Min(normalBodyTemp, current + change);

        sys.ForceChangeTemperature(args.TargetEntity, temp);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds(2), true);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<StatusEffectsSystem>();
        status.TryAddStatusEffect<RMCBlindedComponent>(args.TargetEntity, "Blinded", TimeSpan.FromSeconds(6), true);
    }
}
