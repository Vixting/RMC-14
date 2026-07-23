using System.Diagnostics.CodeAnalysis;
using Content.Server._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Xenonids.Biomass;
using Content.Server.GameTicking.Events;
using Robust.Server.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Xenonids.Biomass;

public sealed class RMCBiomassAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly RMCChemistryResearchSystem _chemResearch = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private static readonly EntProtoId<RMCBiomassAnalyzerComponent> AnalyzerProto = "RMCBiomassAnalyzerData";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        EnsureAnalyzer();
    }

    public Entity<RMCBiomassAnalyzerComponent> EnsureAnalyzer()
    {
        if (TryGetAnalyzer(out var analyzer))
            return analyzer.Value;

        var id = Spawn(AnalyzerProto);
        var comp = EnsureComp<RMCBiomassAnalyzerComponent>(id);
        _pvsOverride.AddGlobalOverride(id);

        return (id, comp);
    }

    public bool TryGetAnalyzer([NotNullWhen(true)] out Entity<RMCBiomassAnalyzerComponent>? analyzer)
    {
        var query = EntityQueryEnumerator<RMCBiomassAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            analyzer = (uid, comp);
            return true;
        }

        analyzer = default;
        return false;
    }

    public void AddPoints(int amount)
    {
        var analyzer = EnsureAnalyzer();
        analyzer.Comp.Points += amount;
        Dirty(analyzer);
    }

    public bool TrySpendPoints(int cost)
    {
        var analyzer = EnsureAnalyzer();
        if (cost > analyzer.Comp.Points)
            return false;

        analyzer.Comp.Points -= cost;
        Dirty(analyzer);
        return true;
    }

    public int SetPoints(int amount)
    {
        var analyzer = EnsureAnalyzer();
        analyzer.Comp.Points = Math.Max(amount, 0);
        Dirty(analyzer);
        return analyzer.Comp.Points;
    }

    public bool HasSufficientClearance(RMCBiomassUpgradePrototype upgrade)
    {
        return _chemResearch.HasSufficientClearance(upgrade.RequiredClearance);
    }

    public int GetCurrentCost(RMCBiomassUpgradePrototype upgrade)
    {
        var count = TryGetAnalyzer(out var analyzer)
            ? analyzer.Value.Comp.PurchaseCounts.GetValueOrDefault(upgrade.ID)
            : 0;

        return Math.Clamp(upgrade.Cost + upgrade.PriceChange * count, upgrade.MinimumPrice, upgrade.MaximumPrice);
    }

    public bool TryPurchaseUpgrade(string id, EntityCoordinates spawnAt, out string? failReason)
    {
        failReason = null;

        if (!_prototype.TryIndex<RMCBiomassUpgradePrototype>(id, out var upgrade))
        {
            failReason = "Unknown upgrade.";
            return false;
        }

        var analyzer = EnsureAnalyzer();

        if (!_chemResearch.HasSufficientClearance(upgrade.RequiredClearance))
        {
            failReason = "Insufficient clearance.";
            return false;
        }

        var cost = GetCurrentCost(upgrade);
        if (!TrySpendPoints(cost))
        {
            failReason = "Insufficient biomass points.";
            return false;
        }

        analyzer.Comp.PurchaseCounts[id] = analyzer.Comp.PurchaseCounts.GetValueOrDefault(id) + 1;
        Dirty(analyzer);

        if (upgrade.PrintedItem != null)
            Spawn(upgrade.PrintedItem, spawnAt);

        return true;
    }
}
