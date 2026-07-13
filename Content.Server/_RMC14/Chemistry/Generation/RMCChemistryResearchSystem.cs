using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Content.Server.Radio.EntitySystems;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Intel;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Robust.Server.GameStates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCChemistryResearchSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCChemicalGeneratorSystem _generator = default!;
    [Dependency] private readonly IntelSystem _intel = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Its medhud wearers in CM but I'm lazy & this makes more sense :godo:
    private static readonly ProtoId<RadioChannelPrototype> MedicalAdvisoryChannel = "MarineMedical";

    private static readonly EntProtoId<RMCChemistryResearchComponent> ResearchProto = "RMCChemistryResearch";
    private static readonly EntProtoId ContractNotesProto = "RMCChemContractNotes";

    private const int ResearchLevelIncreaseMultiplier = 3;
    public const int TechtreeLevelMultiplier = 2;
    public const int MaxClearanceLevel = 5;

    public const int ContractSlotCount = 3;
    private static readonly TimeSpan ContractRerollNotPicked = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ContractRerollPicked = TimeSpan.FromMinutes(6);

    private readonly Dictionary<int, ContractStats> _contractStats = new();

    public override void Update(float frameTime)
    {
        if (!TryGetResearch(out var research))
            return;

        if (_timing.CurTime < research.Value.Comp.NextReroll)
            return;

        RerollContracts(research.Value);
    }

    public Entity<RMCChemistryResearchComponent> EnsureResearch()
    {
        if (TryGetResearch(out var research))
        {
            Log.Debug($"EnsureResearch: found existing {ToPrettyString(research.Value.Owner)}, Contracts.Count={research.Value.Comp.Contracts.Count}");
            return research.Value;
        }

        var id = Spawn(ResearchProto);
        var comp = EnsureComp<RMCChemistryResearchComponent>(id);
        var ent = (Entity<RMCChemistryResearchComponent>) (id, comp);
        _pvsOverride.AddGlobalOverride(id);
        Log.Debug($"EnsureResearch: no existing singleton found, spawned {ToPrettyString(id)} with PVS global override");
        RerollContracts(ent);

        return ent;
    }

    public bool TryGetResearch([NotNullWhen(true)] out Entity<RMCChemistryResearchComponent>? research)
    {
        var query = EntityQueryEnumerator<RMCChemistryResearchComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            research = (uid, comp);
            return true;
        }

        research = default;
        return false;
    }

    public void TryAwardIdentificationCredits(string reagentId, int tier)
    {
        var research = EnsureResearch();
        if (!research.Comp.IdentifiedIds.Add(reagentId))
            return;

        var reward = tier switch
        {
            1 => 3,
            2 => 5,
            _ => 7,
        };

        research.Comp.Credits += reward;
        Dirty(research);

        var points = tier switch
        {
            1 => FixedPoint2.New(0.1),
            2 => FixedPoint2.New(0.2),
            _ => FixedPoint2.New(0.3),
        };
        _intel.AddAnalyzedChemical(points);
    }

    public bool TrySpendCredits(int cost)
    {
        var research = EnsureResearch();
        if (cost > research.Comp.Credits)
            return false;

        research.Comp.Credits -= cost;
        Dirty(research);
        return true;
    }

    public void AddCredits(int amount)
    {
        var research = EnsureResearch();
        research.Comp.Credits += amount;
        Dirty(research);
    }

    public int SetCredits(int amount)
    {
        var research = EnsureResearch();
        research.Comp.Credits = Math.Max(amount, 0);
        Dirty(research);
        return research.Comp.Credits;
    }

    public int SetClearance(int level)
    {
        var research = EnsureResearch();
        research.Comp.ClearanceLevel = Math.Clamp(level, 1, MaxClearanceLevel);
        Dirty(research);
        return research.Comp.ClearanceLevel;
    }

    public int GetClearanceUpgradeCost(int currentClearance)
    {
        return Math.Max(ResearchLevelIncreaseMultiplier * currentClearance + 1, 1);
    }

    public bool TryRaiseClearance(EntityCoordinates? spawnAt = null)
    {
        var research = EnsureResearch();
        if (research.Comp.ClearanceLevel >= MaxClearanceLevel)
            return false;

        var cost = GetClearanceUpgradeCost(research.Comp.ClearanceLevel);
        if (!TrySpendCredits(cost))
            return false;

        research.Comp.ClearanceLevel++;
        Dirty(research);

        if (spawnAt != null)
        {
            var note = Spawn(ContractNotesProto, spawnAt.Value);
            var sb = new StringBuilder();
            RMCChemPaperFormat.AppendHeader(sb, "Official Weston-Yamada Document", "Clearance Notice");
            sb.AppendLine();
            sb.AppendLine($"[bold][head=3]Clearance Level {research.Comp.ClearanceLevel} Achieved[/head][/bold]");
            sb.AppendLine();
            sb.AppendLine("Congratulations, your research clearance has been elevated. New synthesis and simulation options may now be available. As a token of our appreciation, a complimentary synthesis report has been enclosed.");
            _paper.SetContent(note, RMCChemPaperFormat.Wrap(sb));

            var reagentId = _generator.GenerateReagent(research.Comp.ClearanceLevel);
            if (_rmcReagent.TryIndex(reagentId, out var reagent))
                _generator.PrintReport(spawnAt.Value, reagentId, reagent, category: "Clearance Grants");
        }

        return true;
    }

    // that gates viewing/completing CHEM_CLASS_SPECIAL reagents like xeno plasmas
    public const int XAccessCost = 5;

    public bool HasXAccess()
    {
        return TryGetResearch(out var research) && research.Value.Comp.ReachedXAccess;
    }

    public bool HasSufficientClearance(int tier)
    {
        return TryGetResearch(out var research) && research.Value.Comp.ClearanceLevel >= tier;
    }

    public bool TryRequestXAccess()
    {
        var research = EnsureResearch();
        if (research.Comp.ReachedXAccess)
            return false;

        if (!TrySpendCredits(XAccessCost))
            return false;

        research.Comp.ReachedXAccess = true;
        Dirty(research);
        return true;
    }

    public bool IsLocked(string reagentId)
    {
        return TryGetResearch(out var research) && research.Value.Comp.LockedIds.Contains(reagentId);
    }

    public bool IsIdentified(string reagentId)
    {
        return TryGetResearch(out var research) && research.Value.Comp.IdentifiedIds.Contains(reagentId);
    }

    public void LockLineage(string reagentId)
    {
        var research = EnsureResearch();

        if (!_generator.TryGetOriginalId(reagentId, out var parentId))
        {
            research.Comp.LockedIds.Add(reagentId);
        }
        else
        {
            research.Comp.LockedIds.Add(parentId);
            foreach (var sibling in _generator.GetVariantsOf(parentId))
                research.Comp.LockedIds.Add(sibling);
        }

        Dirty(research);
    }

    public void ForceRerollContracts()
    {
        RerollContracts(EnsureResearch());
    }

    public void RerollContracts(Entity<RMCChemistryResearchComponent> research)
    {
        Log.Debug($"RerollContracts: starting for {ToPrettyString(research.Owner)}");
        research.Comp.Contracts.Clear();
        _contractStats.Clear();

        for (var i = 0; i < ContractSlotCount; i++)
        {
            try
            {
                var tier = _random.Next(1, 4);
                var stats = _generator.RollStats(tier);
                _contractStats[i] = stats;
                research.Comp.Contracts.Add(new ContractSlot
                {
                    Name = stats.Name,
                    Tier = tier,
                    PropertyHintId = stats.PropertyHintId,
                    IngredientHintId = stats.IngredientHintId,
                    Taken = false,
                });
                Log.Debug($"RerollContracts: slot {i} rolled '{stats.Name}' tier {tier}");
            }
            catch (Exception e)
            {
                Log.Error($"RerollContracts: slot {i} failed to roll:\n{e}");
            }
        }

        research.Comp.PickedThisCycle = false;
        research.Comp.NextReroll = _timing.CurTime + ContractRerollNotPicked;
        Dirty(research);
        Log.Debug($"RerollContracts: finished, Contracts.Count={research.Comp.Contracts.Count}, NextReroll={research.Comp.NextReroll}");

        var computers = EntityQueryEnumerator<RMCResearchComputerComponent>();
        while (computers.MoveNext(out var computerId, out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-research-computer-contracts-updated"), computerId, PopupType.Medium);
            _audio.PlayPvs("/Audio/Machines/twobeep.ogg", computerId);
        }
    }

    public bool TryTakeContract(int slotIndex, EntityCoordinates spawnAt)
    {
        var research = EnsureResearch();
        if (research.Comp.PickedThisCycle)
            return false;

        if (slotIndex < 0 || slotIndex >= research.Comp.Contracts.Count)
            return false;

        var slot = research.Comp.Contracts[slotIndex];
        if (slot.Taken || !_contractStats.TryGetValue(slotIndex, out var stats))
            return false;

        var (reagentId, ingredients) = _generator.FinalizeContract(stats, slot.Tier);

        slot.Taken = true;
        research.Comp.PickedThisCycle = true;
        research.Comp.NextReroll = _timing.CurTime + ContractRerollPicked;

        var notes = Spawn(ContractNotesProto, spawnAt);
        _paper.SetContent(notes, FormatContractNotes(reagentId, ingredients, stats.Properties, slot.Tier, research.Comp.ClearanceLevel));
        _audio.PlayPvs("/Audio/_RMC14/Machines/fax.ogg", notes);
        research.Comp.LastPickedContract = GetNetEntity(notes);

        Dirty(research);
        return true;
    }

    public bool TryReprintContract(EntityCoordinates spawnAt)
    {
        var research = EnsureResearch();
        if (research.Comp.LastPickedContract is not { } netEntity ||
            GetEntity(netEntity) is not { Valid: true } original ||
            !TryComp(original, out PaperComponent? originalPaper))
        {
            return false;
        }

        var notes = Spawn(ContractNotesProto, spawnAt);
        _paper.SetContent(notes, originalPaper.Content);

        return true;
    }

    private static readonly char[] ExperimentPrefixes = ['C', 'Q', 'V', 'W', 'X', 'Y', 'Z'];
    private static readonly char[] ExperimentSuffixes = ['a', 'b', 'c'];

    private string FormatContractNotes(
        ProtoId<ReagentPrototype> reagentId,
        List<(string Id, int Amount, bool Catalyst)> ingredients,
        List<(ChemGeneratorPropertyPrototype Property, int Level)> properties,
        int tier,
        int clearanceLevel)
    {
        var name = GetReagentName(reagentId);
        var experimentId = $"{_random.Pick(ExperimentPrefixes)}{_random.Next(100, 1000)}{_random.Pick(ExperimentSuffixes)}";

        var sb = new StringBuilder();
        RMCChemPaperFormat.AppendHeader(sb, "Official Weston-Yamada Document", "Experiment Notes");
        sb.AppendLine();
        sb.AppendLine($"[bold][head=3][color=#4A90E2]Contract for {name}[/color][/head][/bold]");
        sb.AppendLine();

        if (clearanceLevel < tier)
        {
            sb.AppendLine($"[bold]CLASSIFIED: Clearance Level {tier} Required[/bold]");
            sb.AppendLine();
            sb.AppendLine("Formula and composition data withheld pending clearance elevation.");
            sb.AppendLine();
            RMCChemPaperFormat.AppendFooter(sb, "Weston-Yamada");
            return RMCChemPaperFormat.Wrap(sb);
        }

        sb.AppendLine($"During experiment {experimentId} the theorized compound identified as {name} was successfully synthesized using the following formula:");
        sb.AppendLine();

        foreach (var (id, amount, catalyst) in ingredients.Where(i => !i.Catalyst))
            sb.AppendLine($"- {amount} {GetReagentName(id)}");

        var catalysts = ingredients.Where(i => i.Catalyst).ToList();
        if (catalysts.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("While using the following catalysts:");
            sb.AppendLine();
            foreach (var (id, amount, _) in catalysts)
                sb.AppendLine($"- {amount} {GetReagentName(id)}");
        }

        sb.AppendLine();
        sb.AppendLine("Testing for chemical properties is currently pending.");

        if (RMCChemPaperFormat.IsVolatile(properties))
        {
            sb.AppendLine();
            sb.AppendLine("[bold]WARNING: UNSTABLE REAGENT. MIX CAREFULLY.[/bold]");
        }

        sb.AppendLine();
        RMCChemPaperFormat.AppendFooter(sb, "Weston-Yamada");

        return RMCChemPaperFormat.Wrap(sb);
    }

    private string GetReagentName(string reagentId)
    {
        return _rmcReagent.TryIndex(reagentId, out var reagent) ? reagent.LocalizedName : reagentId;
    }

    public void SaveDocument(string category, EntityUid report, string title)
    {
        var research = EnsureResearch();
        if (!research.Comp.Documents.TryGetValue(category, out var list))
            research.Comp.Documents[category] = list = new List<DocumentEntry>();

        list.Add(new DocumentEntry { Report = GetNetEntity(report), Title = title, Time = _timing.CurTime });
        Dirty(research);
    }

    public bool PublishDocument(string category, string title)
    {
        var research = EnsureResearch();
        if (!research.Comp.Documents.TryGetValue(category, out var list))
            return false;

        var entry = list.FirstOrDefault(e => e.Title == title);
        if (entry == null)
            return false;

        if (!research.Comp.Published.TryGetValue(category, out var published))
            research.Comp.Published[category] = published = new List<DocumentEntry>();

        published.Add(new DocumentEntry { Report = entry.Report, Title = entry.Title, Time = entry.Time });
        Dirty(research);
        return true;
    }

    public bool UnpublishDocument(string category, string title)
    {
        var research = EnsureResearch();
        if (!research.Comp.Published.TryGetValue(category, out var list))
            return false;

        var compound = GetCompoundName(title);
        var removed = list.RemoveAll(e => GetCompoundName(e.Title) == compound);
        Dirty(research);
        return removed > 0;
    }

    private static string GetCompoundName(string title)
    {
        var dashIndex = title.IndexOf(" - ", StringComparison.Ordinal);
        return dashIndex < 0 ? title : title[(dashIndex + 3)..];
    }

    public bool TryGetDocument(string category, string title, bool published, out NetEntity report)
    {
        report = default;
        if (!TryGetResearch(out var research))
            return false;

        var dict = published ? research.Value.Comp.Published : research.Value.Comp.Documents;
        if (!dict.TryGetValue(category, out var list))
            return false;

        var entry = list.FirstOrDefault(e => e.Title == title);
        if (entry == null)
            return false;

        report = entry.Report;
        return true;
    }

    private static readonly TimeSpan AnnounceCooldown = TimeSpan.FromMinutes(4);

    public void AnnounceDocument(EntityUid computer, string category, string title, EntityUid actor)
    {
        if (!TryGetDocument(category, title, published: true, out var netEntity))
        {
            _popup.PopupEntity("The document must be published to announce it.", computer, actor);
            return;
        }

        var research = EnsureResearch();
        if (_timing.CurTime < research.Comp.NextAnnounceAt)
        {
            _popup.PopupEntity("The announcement system is still recharging.", computer, actor);
            return;
        }

        if (GetEntity(netEntity) is not { Valid: true } report ||
            !TryComp(report, out RMCChemResearchReportComponent? reportComp))
        {
            return;
        }

        var name = GetReagentName(reportComp.ReagentId);
        var properties = reportComp.Properties.Count > 0
            ? string.Join(", ", reportComp.Properties.Select(p => $"{p.PropertyId} {p.Level}"))
            : "none detected";

        var message = $"Chemical Advisory: {name} has been identified. Notable properties: {properties}.";
        _radio.SendRadioMessage(computer, message, MedicalAdvisoryChannel, computer);
        _popup.PopupEntity(Loc.GetString("rmc-research-computer-announce-sent"), computer, actor);

        research.Comp.NextAnnounceAt = _timing.CurTime + AnnounceCooldown;
        Dirty(research);
    }

    public bool TryPrintDocument(string category, string title, bool published, EntityCoordinates spawnAt)
    {
        if (!TryGetDocument(category, title, published, out var netEntity) ||
            GetEntity(netEntity) is not { Valid: true } original ||
            !TryComp(original, out PaperComponent? originalPaper))
        {
            return false;
        }

        var protoId = MetaData(original).EntityPrototype?.ID ?? ContractNotesProto.Id;
        var copy = Spawn(protoId, spawnAt);
        _paper.SetContent(copy, originalPaper.Content);
        return true;
    }

    public bool TrySaveManualScan(EntityUid item, EntityCoordinates spawnAt)
    {
        if (!TryComp(item, out PaperComponent? paper))
            return false;

        var protoId = MetaData(item).EntityPrototype?.ID;
        if (protoId == null)
            return false;

        var copy = Spawn(protoId, spawnAt);
        _paper.SetContent(copy, paper.Content);
        SaveDocument("Manual Scans", copy, Name(item));

        return true;
    }
}
