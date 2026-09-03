using Content.Shared._RMC14.Botany;
using Content.Shared.Atmos;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Gas consumption from, and production into, the tray's containing atmosphere.
/// </summary>
public sealed class RMCConsumeExudeGasSystem : EntitySystem
{
    public const float HydroponicsSpeedMultiplier = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCPlantComponent, RMCPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<RMCPlantComponent> ent, ref RMCPlantGrowEvent args)
    {
        var tray = Comp<RMCPlantTrayComponent>(args.Tray);
        TickConsume(ent, tray, args.Environment);
        TickExude(ent, args.Environment);
    }

    public void TickConsume(Entity<RMCPlantComponent> plant, RMCPlantTrayComponent tray, GasMixture environment)
    {
        tray.MissingGas = 0;

        if (!TryComp(plant.Owner, out RMCConsumeExudeGasComponent? gas) || gas.ConsumeGasses.Count == 0)
            return;

        foreach (var (gasType, amount) in gas.ConsumeGasses)
        {
            if (environment.GetMoles(gasType) < amount)
            {
                tray.MissingGas++;
                continue;
            }

            environment.AdjustMoles(gasType, -amount);
        }

        if (tray.MissingGas > 0)
        {
            plant.Comp.Health -= tray.MissingGas * HydroponicsSpeedMultiplier;
            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;
        }
    }

    public void TickExude(EntityUid plant, GasMixture environment)
    {
        if (!TryComp(plant, out RMCConsumeExudeGasComponent? gas) || gas.ExudeGasses.Count == 0)
            return;

        var potency = TryComp(plant, out RMCPlantChemicalsComponent? chemicals) ? chemicals.Potency : 0f;
        var exudeCount = gas.ExudeGasses.Count;

        foreach (var (gasType, amount) in gas.ExudeGasses)
        {
            environment.AdjustMoles(gasType,
                MathF.Max(1f, MathF.Round(amount * MathF.Round(potency) / exudeCount)));
        }
    }
}
