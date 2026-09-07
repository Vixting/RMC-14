using Content.Shared._RMC14.Botany;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Pest population growth, carnivorous consumption, & pest tolerance damage
/// </summary>
public sealed class RMCPlantPestSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public const float HydroponicsSpeedMultiplier = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(ref RMCPlantGrowEvent args)
    {
        if (!TryComp(args.Plant, out RMCPlantComponent? plantComp))
            return;

        var tray = Comp<RMCPlantTrayComponent>(args.Tray);
        var ent = (args.Plant, plantComp);
        TickSpawn(tray);
        Tick(ent, tray, Comp<RMCPlantTraitsComponent>(args.Plant));
    }

    /// <summary>
    /// Small chance for the pest population to increase. Only runs for a live, non-dead plant.
    /// </summary>
    public void TickSpawn(RMCPlantTrayComponent tray)
    {
        if (!_random.Prob(0.01f))
            return;

        tray.PestLevel += 0.5f * HydroponicsSpeedMultiplier;
        if (tray.DrawWarnings)
            tray.UpdateSpriteAfterUpdate = true;
    }

    public void Tick(Entity<RMCPlantComponent> plant, RMCPlantTrayComponent tray, RMCPlantTraitsComponent traits)
    {
        if (tray.PestLevel <= 0)
            return;

        if (HasComp<RMCPlantTraitCarnivorousComponent>(plant.Owner))
        {
            tray.PestLevel -= HydroponicsSpeedMultiplier;
            plant.Comp.Health += HydroponicsSpeedMultiplier;
        }
        else if (tray.PestLevel > traits.PestTolerance)
        {
            plant.Comp.Health -= HydroponicsSpeedMultiplier;
        }

        if (tray.DrawWarnings)
            tray.UpdateSpriteAfterUpdate = true;
    }
}
