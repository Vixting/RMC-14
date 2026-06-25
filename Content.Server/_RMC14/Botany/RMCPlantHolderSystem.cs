using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared._RMC14.Botany;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Botany;

public sealed class RMCPlantHolderSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantHolderComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
        SubscribeLocalEvent<RMCAutoHarvestUpgradeComponent, AfterInteractEvent>(OnUpgradeInteract);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCAutoHarvestComponent, PlantHolderComponent>();
        while (query.MoveNext(out var uid, out _, out var holder))
        {
            if (!holder.Harvest || holder.Dead || holder.Seed == null)
                continue;

            _plantHolder.AutoHarvest(uid, holder);
        }
    }

    private void OnGetVerbs(Entity<PlantHolderComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;

        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("rmc-tray-flush-verb"),
            Priority = -1,
            Act = () => FlushTray(ent, user),
        });
    }

    private void FlushTray(Entity<PlantHolderComponent> ent, EntityUid user)
    {
        var comp = ent.Comp;

        if (comp.WaterLevel <= 0 && comp.NutritionLevel <= 0 && comp.Toxins <= 0)
        {
            _popup.PopupCursor(Loc.GetString("rmc-tray-flush-empty"), user);
            return;
        }

        comp.WaterLevel = 0;
        comp.NutritionLevel = 0;
        comp.Toxins = 0;

        if (_solution.ResolveSolution(ent.Owner, comp.SoilSolutionName, ref comp.SoilSolution, out var soilSoln))
            _solution.RemoveAllSolution(comp.SoilSolution.Value);

        _plantHolder.CheckLevelSanity(ent);
        _popup.PopupCursor(Loc.GetString("rmc-tray-flush-success"), user);
    }

    private void OnUpgradeInteract(Entity<RMCAutoHarvestUpgradeComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (!TryComp(args.Target, out PlantHolderComponent? _))
            return;

        args.Handled = true;

        if (HasComp<RMCAutoHarvestComponent>(args.Target))
        {
            _popup.PopupCursor(Loc.GetString("rmc-auto-harvest-already-installed"), args.User);
            return;
        }

        AddComp<RMCAutoHarvestComponent>(args.Target.Value);
        QueueDel(ent);
        _popup.PopupCursor(Loc.GetString("rmc-auto-harvest-installed"), args.User);
    }
}
