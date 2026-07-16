using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Xenonids.Biomass;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Biomass;

[UsedImplicitly]
public sealed class RMCBiomassAnalyzerBui : BoundUserInterface, IRefreshableBui
{
    private static readonly Color Green = Color.FromHex("#3FBF3F");
    private static readonly Color GreenDim = Color.FromHex("#2A8A2A");
    private static readonly Color GreenBlack = Color.FromHex("#0A1F0A");

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private RMCBiomassAnalyzerWindow? _window;
    private readonly List<TabButtonControl> _tabButtons = new();

    public RMCBiomassAnalyzerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCBiomassAnalyzerWindow>();

        BuildTabBar();
        Refresh();
    }

    private void BuildTabBar()
    {
        _tabButtons.Clear();
        _window!.TabBar.RemoveAllChildren();

        AddTabButton("Machinery");
        AddTabButton("Items");
        AddTabButton("Armor");

        SetActiveTab(_window.Tabs.CurrentTab);
        _window.Tabs.OnTabChanged += SetActiveTab;
    }

    private void AddTabButton(string text)
    {
        var index = _tabButtons.Count;
        var tab = new TabButtonControl(text);
        tab.OnPressed += _ => _window!.Tabs.CurrentTab = index;
        _window!.TabBar.AddChild(tab);
        _tabButtons.Add(tab);
    }

    private void SetActiveTab(int index)
    {
        for (var i = 0; i < _tabButtons.Count; i++)
        {
            _tabButtons[i].SetActive(i == index);
        }
    }

    private sealed class TabButtonControl : Button
    {
        private readonly Label _label;
        private bool _active;

        public TabButtonControl(string text)
        {
            HorizontalExpand = true;
            Modulate = Color.White;
            ModulateSelfOverride = Color.White;
            StyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.Transparent,
                BorderColor = GreenDim,
                BorderThickness = new Thickness(4),
            };

            _label = new Label { Text = text, HorizontalAlignment = HAlignment.Center, Margin = new Thickness(6) };
            AddChild(_label);

            SetActive(false);
        }

        public void SetActive(bool active)
        {
            _active = active;

            var box = (StyleBoxFlat) StyleBoxOverride!;
            box.BackgroundColor = active ? Green : Color.Transparent;
            _label.FontColorOverride = active ? GreenBlack : Green;
        }
    }

    private bool TryGetAnalyzer(out RMCBiomassAnalyzerComponent analyzer)
    {
        var query = _entity.EntityQueryEnumerator<RMCBiomassAnalyzerComponent>();
        if (query.MoveNext(out _, out var comp))
        {
            analyzer = comp;
            return true;
        }

        analyzer = default!;
        return false;
    }

    private int GetClearanceLevel()
    {
        var query = _entity.EntityQueryEnumerator<RMCChemistryResearchComponent>();
        if (query.MoveNext(out _, out var comp))
            return comp.ClearanceLevel;

        return 1;
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!TryGetAnalyzer(out var analyzer))
            return;

        var clearance = GetClearanceLevel();

        _window.PointsLabel.Text = $"Biomass points available: {analyzer.Points}";

        PopulateCategory(_window.MachineryBox, RMCBiomassUpgradeCategory.Machinery, analyzer, clearance);
        PopulateCategory(_window.ItemsBox, RMCBiomassUpgradeCategory.Items, analyzer, clearance);
        PopulateCategory(_window.ArmorBox, RMCBiomassUpgradeCategory.Armor, analyzer, clearance);
    }

    private void PopulateCategory(BoxContainer box, RMCBiomassUpgradeCategory category, RMCBiomassAnalyzerComponent analyzer, int clearance)
    {
        var upgrades = _prototype.EnumeratePrototypes<RMCBiomassUpgradePrototype>()
            .Where(u => u.Category == category)
            .OrderBy(u => u.RequiredClearance)
            .ThenBy(u => u.Cost)
            .ToList();

        var index = 0;
        foreach (var upgrade in upgrades)
        {
            UpgradeCard card;
            if (index < box.ChildCount && box.GetChild(index) is UpgradeCard existing)
            {
                card = existing;
            }
            else
            {
                card = new UpgradeCard();
                box.AddChild(card);
            }

            var purchased = upgrade.Kind != RMCBiomassUpgradeKind.ResearchContractReroll &&
                             analyzer.PurchasedUpgrades.Contains(upgrade.ID);
            var canAfford = analyzer.Points >= upgrade.Cost && clearance >= upgrade.RequiredClearance;

            card.NameLabel.Text = upgrade.Name;
            card.DescriptionLabel.Text = upgrade.Description;
            card.CostLabel.Text = $"Cost: {upgrade.Cost} biomass | Clearance {upgrade.RequiredClearance}";
            card.PurchaseButton.Disabled = purchased || !canAfford;
            card.PurchaseButton.Text = purchased ? "Purchased" : "Purchase";
            card.UpgradeId = upgrade.ID;
            card.OnPurchase = id => SendPredictedMessage(new RMCBiomassAnalyzerPurchaseUpgradeBuiMsg(id));

            index++;
        }

        box.RemoveChildrenAfter(index);
    }

    private sealed class UpgradeCard : BoxContainer
    {
        public readonly Label NameLabel;
        public readonly Label DescriptionLabel;
        public readonly Label CostLabel;
        public readonly Button PurchaseButton;

        public string UpgradeId = string.Empty;
        public Action<string>? OnPurchase;

        public UpgradeCard()
        {
            Orientation = LayoutOrientation.Vertical;
            HorizontalExpand = true;

            var panel = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = GreenBlack, BorderColor = Green, BorderThickness = new Thickness(1) },
            };

            var inner = new BoxContainer { Orientation = LayoutOrientation.Vertical, Margin = new Thickness(6) };

            NameLabel = new Label { FontColorOverride = Green };
            DescriptionLabel = new Label { FontColorOverride = GreenDim };
            CostLabel = new Label { FontColorOverride = GreenDim };
            PurchaseButton = new Button
            {
                HorizontalExpand = true,
                Margin = new Thickness(0, 4, 0, 0),
                Modulate = Color.White,
                ModulateSelfOverride = Color.White,
                StyleBoxOverride = new StyleBoxFlat
                {
                    BackgroundColor = Green,
                    BorderColor = GreenDim,
                    BorderThickness = new Thickness(2),
                },
            };
            PurchaseButton.Label.FontColorOverride = GreenBlack;

            inner.AddChild(NameLabel);
            inner.AddChild(DescriptionLabel);
            inner.AddChild(CostLabel);
            inner.AddChild(PurchaseButton);

            panel.AddChild(inner);
            AddChild(panel);

            PurchaseButton.OnPressed += _ => OnPurchase?.Invoke(UpgradeId);
        }
    }
}
