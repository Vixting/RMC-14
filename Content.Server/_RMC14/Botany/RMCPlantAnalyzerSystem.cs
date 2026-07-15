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
            HarvestType.Repeat => "Repeating",
            HarvestType.SelfHarvest => "Self-harvesting",
            _ => "No repeat",
        };

        var msg = $"[color=green]{name}[/color]\n" +
            $"Endurance: {(int)seed.Endurance}  Yield: {seed.Yield}  Potency: {(int)seed.Potency}\n" +
            $"Lifespan: {(int)seed.Lifespan}  Maturation: {(int)seed.Maturation}  Production: {(int)seed.Production}\n" +
            $"Harvest: {harvest}  Seedless: {seed.Seedless}  Viable: {seed.Viable}";

        _popup.PopupCursor(msg, user, PopupType.Large);

        ShowChemicals(user, seed);
    }

    private void ShowPlantHolderInfo(EntityUid user, PlantHolderComponent holder)
    {
        if (holder.Seed == null)
        {
            _popup.PopupCursor("The tray is empty.", user);
            return;
        }

        var seed = holder.Seed;
        var name = Loc.GetString(seed.DisplayName);
        var harvest = seed.HarvestRepeat switch
        {
            HarvestType.Repeat => "Repeating",
            HarvestType.SelfHarvest => "Self-harvesting",
            _ => "No repeat",
        };

        var msg = $"[color=green]{name}[/color]\n" +
            $"Health: {(int)holder.Health}/{(int)seed.Endurance}  Water: {(int)holder.WaterLevel}  Nutrition: {(int)holder.NutritionLevel}\n" +
            $"Pest: {(int)holder.PestLevel}  Weed: {(int)holder.WeedLevel}  Toxin: {(int)holder.Toxins}\n" +
            $"Yield: {seed.Yield}  Potency: {(int)seed.Potency}  Lifespan: {(int)seed.Lifespan}\n" +
            $"Maturation: {(int)seed.Maturation}  Production: {(int)seed.Production}\n" +
            $"Harvest: {harvest}  Seedless: {seed.Seedless}  Viable: {seed.Viable}";

        _popup.PopupCursor(msg, user, PopupType.Large);

        ShowChemicals(user, seed);
    }

    private void ShowProduceInfo(EntityUid user, SeedData seed)
    {
        var name = Loc.GetString(seed.DisplayName);
        var msg = $"[color=green]{name}[/color]\nPotency: {(int)seed.Potency}";

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
            chemLines.Add($"• {reagentId} ({qty.Min}–{qty.Max} units)");
        }

        var chemMsg = $"Chemicals:\n{string.Join("\n", chemLines)}";
        _popup.PopupCursor(chemMsg, user, PopupType.Large);
    }
}
