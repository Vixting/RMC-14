using Content.Server.Botany.Components;
using Content.Shared.Botany;
using Content.Shared.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared._RMC14.Botany;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.Botany;

public sealed class RMCPlantAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<RMCPlantAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        _audio.PlayPvs(ent.Comp.ScanSound, ent);

        if (TryComp(args.Target, out SeedComponent? seedComp) && _botany.TryGetSeed(seedComp, out var seed))
        {
            ShowSeedInfo(args.User, seed);
            args.Handled = true;
            return;
        }

        if (TryComp(args.Target, out PlantHolderComponent? holder))
        {
            ShowPlantHolderInfo(args.User, holder);
            args.Handled = true;
            return;
        }

        if (TryComp(args.Target, out ProduceComponent? produce) && _botany.TryGetSeed(produce, out var produceSeed))
        {
            ShowProduceInfo(args.User, produceSeed);
            args.Handled = true;
        }
    }

    private void ShowSeedInfo(EntityUid user, SeedData seed)
    {
        var name = Loc.GetString(seed.DisplayName);
        var harvest = seed.HarvestRepeat switch
        {
            HarvestType.Repeat => Loc.GetString("rmc-plant-analyzer-harvest-repeat"),
            HarvestType.SelfHarvest => Loc.GetString("rmc-plant-analyzer-harvest-self"),
            _ => Loc.GetString("rmc-plant-analyzer-harvest-none"),
        };

        var msg = Loc.GetString("rmc-plant-analyzer-seed-info",
            ("name", name),
            ("endurance", (int)seed.Endurance),
            ("yield", seed.Yield),
            ("lifespan", (int)seed.Lifespan),
            ("maturation", (int)seed.Maturation),
            ("production", (int)seed.Production),
            ("potency", (int)seed.Potency),
            ("harvest", harvest),
            ("seedless", seed.Seedless),
            ("viable", seed.Viable));

        _popup.PopupCursor(msg, user, PopupType.Large);

        ShowChemicals(user, seed);
    }

    private void ShowPlantHolderInfo(EntityUid user, PlantHolderComponent holder)
    {
        if (holder.Seed == null)
        {
            _popup.PopupCursor(Loc.GetString("rmc-plant-analyzer-empty-tray"), user);
            return;
        }

        var seed = holder.Seed;
        var name = Loc.GetString(seed.DisplayName);
        var harvest = seed.HarvestRepeat switch
        {
            HarvestType.Repeat => Loc.GetString("rmc-plant-analyzer-harvest-repeat"),
            HarvestType.SelfHarvest => Loc.GetString("rmc-plant-analyzer-harvest-self"),
            _ => Loc.GetString("rmc-plant-analyzer-harvest-none"),
        };

        var msg = Loc.GetString("rmc-plant-analyzer-tray-info",
            ("name", name),
            ("health", (int)holder.Health),
            ("endurance", (int)seed.Endurance),
            ("water", (int)holder.WaterLevel),
            ("nutrition", (int)holder.NutritionLevel),
            ("pest", (int)holder.PestLevel),
            ("weed", (int)holder.WeedLevel),
            ("toxin", (int)holder.Toxins),
            ("yield", seed.Yield),
            ("lifespan", (int)seed.Lifespan),
            ("maturation", (int)seed.Maturation),
            ("production", (int)seed.Production),
            ("potency", (int)seed.Potency),
            ("harvest", harvest),
            ("seedless", seed.Seedless),
            ("viable", seed.Viable));

        _popup.PopupCursor(msg, user, PopupType.Large);

        ShowChemicals(user, seed);
    }

    private void ShowProduceInfo(EntityUid user, SeedData seed)
    {
        var name = Loc.GetString(seed.DisplayName);
        var msg = Loc.GetString("rmc-plant-analyzer-produce-info",
            ("name", name),
            ("potency", (int)seed.Potency));

        _popup.PopupCursor(msg, user, PopupType.Large);

        ShowChemicals(user, seed);
    }

    private void ShowChemicals(EntityUid user, SeedData seed)
    {
        if (seed.Chemicals.Count == 0)
            return;

        var chemLines = new List<string>();
        foreach (var (reagentId, qty) in seed.Chemicals)
        {
            chemLines.Add(Loc.GetString("rmc-plant-analyzer-chem-entry",
                ("reagent", reagentId),
                ("min", qty.Min),
                ("max", qty.Max)));
        }

        var chemMsg = Loc.GetString("rmc-plant-analyzer-chemicals",
            ("chemicals", string.Join("\n", chemLines)));
        _popup.PopupCursor(chemMsg, user, PopupType.Large);
    }
}
