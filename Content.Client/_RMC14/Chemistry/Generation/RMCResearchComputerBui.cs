using System.Linq;
using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Content.Client.Resources;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Chemistry.Generation;

[UsedImplicitly]
public sealed class RMCResearchComputerBui : BoundUserInterface, IRefreshableBui
{
    private const string IconDir = "/Textures/_RMC14/Interface/Icons/";

    private const string AnnounceIcon = IconDir + "dark/radiation.svg.192dpi.png";
    private const string AnnounceIconWhite = IconDir + "white/radiation.svg.192dpi.png";

    private const string BookIcon = IconDir + "dark/book.svg.192dpi.png";
    private const string BookIconWhite = IconDir + "white/book.svg.192dpi.png";
    private const string BookIconYellow = IconDir + "yellow/book.svg.192dpi.png";

    private const string FlaskIcon = IconDir + "dark/flask.svg.192dpi.png";
    private const string FlaskIconWhite = IconDir + "white/flask.svg.192dpi.png";
    private const string FlaskIconYellow = IconDir + "yellow/flask.svg.192dpi.png";

    private const string GearIcon = IconDir + "dark/gear.svg.192dpi.png";
    private const string GearIconWhite = IconDir + "white/gear.svg.192dpi.png";
    private const string GearIconYellow = IconDir + "yellow/gear.svg.192dpi.png";

    private const string PrintIcon = IconDir + "dark/print.svg.192dpi.png";
    private const string PrintIconWhite = IconDir + "white/print.svg.192dpi.png";
    private const string PrintIconYellow = IconDir + "yellow/print.svg.192dpi.png";

    private const string RepeatIcon = IconDir + "dark/arrows-rotate.svg.192dpi.png";
    private const string RepeatIconWhite = IconDir + "white/arrows-rotate.svg.192dpi.png";

    private static readonly Color AmberBright = Color.FromHex("#FAC000");
    private static readonly Color AmberDim = Color.FromHex("#C69214");
    private static readonly Color AmberBlack = Color.FromHex("#221801");
    private static readonly Color AmberBorder = Color.FromHex("#8A6800");
    private static readonly Color AmberWashed = Color.FromHex("#7A6A2E");

    private const int ResearchLevelIncreaseMultiplier = 3;
    private const int MaxClearanceLevel = 5;

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private const int XAccessCost = 5;

    private RMCResearchComputerWindow? _window;
    private Label? _brokerClearanceLabel;
    private readonly List<TabButtonControl> _tabButtons = new();
    private Font _boldFont = default!;
    private Font _smallFont = default!;
    private RMCReagentSystem _rmcReagent = default!;

    private enum SortColumn
    {
        Time,
        Compound,
    }

    private SortColumn _documentsSort = SortColumn.Time;
    private bool _documentsSortAscending;
    private SortColumn _publishedSort = SortColumn.Time;
    private bool _publishedSortAscending;

    public RMCResearchComputerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _boldFont = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-Bold.ttf", 12);
        _smallFont = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 8);
        _rmcReagent = _entity.System<RMCReagentSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCResearchComputerWindow>();

        _brokerClearanceLabel = Icon(_window.BrokerClearanceButton, GearIcon, GearIconWhite, "Broker Clearance");
        Icon(_window.ReprintContractButton, RepeatIcon, RepeatIconWhite, "Reprint");

        _window.BrokerClearanceButton.OnPressed += _ =>
        {
            if (TryGetResearch(out var research) && research.ClearanceLevel >= MaxClearanceLevel)
                OnRequestXAccessPressed();
            else
                OnBrokerClearancePressed();
        };
        _window.ReprintContractButton.OnPressed += _ => SendPredictedMessage(new RMCResearchComputerReprintContractBuiMsg());
        _window.HideOldReportsButton.OnPressed += _ => Refresh();
        _window.HideOldReportsButton.Label.FontColorOverride = AmberBright;
        _window.ContractCooldownLabel.FontOverride = _smallFont;

        foreach (var button in new[]
                 {
                     _window.DocumentsSortTimeButton, _window.DocumentsSortCompoundButton,
                     _window.PublishedSortTimeButton, _window.PublishedSortCompoundButton,
                 })
        {
            button.Label.FontColorOverride = AmberBright;
        }

        _window.DocumentsSortTimeButton.OnPressed += _ => SetSort(SortColumn.Time, ref _documentsSort, ref _documentsSortAscending);
        _window.DocumentsSortCompoundButton.OnPressed += _ => SetSort(SortColumn.Compound, ref _documentsSort, ref _documentsSortAscending);
        _window.PublishedSortTimeButton.OnPressed += _ => SetSort(SortColumn.Time, ref _publishedSort, ref _publishedSortAscending);
        _window.PublishedSortCompoundButton.OnPressed += _ => SetSort(SortColumn.Compound, ref _publishedSort, ref _publishedSortAscending);

        BuildTabBar();

        Refresh();
    }

    private void SetSort(SortColumn column, ref SortColumn current, ref bool ascending)
    {
        if (current == column)
            ascending = !ascending;
        else
        {
            current = column;
            ascending = true;
        }

        Refresh();
    }

    private static IEnumerable<(string Category, DocumentEntry Entry)> ApplySort(
        IEnumerable<(string Category, DocumentEntry Entry)> flattened,
        SortColumn column,
        bool ascending)
    {
        var ordered = column == SortColumn.Time
            ? flattened.OrderBy(x => x.Entry.Time)
            : flattened.OrderBy(x => GetCompoundName(x.Entry.Title).ToUpperInvariant());

        return ascending ? ordered : ordered.Reverse();
    }

    private static void UpdateSortButtonText(Button timeButton, Button compoundButton, string timeLabel, SortColumn column, bool ascending)
    {
        var arrow = ascending ? "▲" : "▼";
        timeButton.Text = column == SortColumn.Time ? $"{timeLabel} {arrow}" : timeLabel;
        compoundButton.Text = column == SortColumn.Compound ? $"Compound {arrow}" : "Compound";
    }

    private static void SetPurchasableStyle(Button button, bool canAfford, Color? disabledColor = null)
    {
        if (button.StyleBoxOverride is not StyleBoxFlat box)
            return;

        box.BackgroundColor = canAfford ? AmberBright : disabledColor ?? Color.Transparent;
    }

    private void BuildTabBar()
    {
        _tabButtons.Clear();
        _window!.TabBar.RemoveAllChildren();

        AddTabButton(GearIcon, GearIconWhite, GearIconYellow, "Manage", "Research");
        AddTabButton(FlaskIcon, FlaskIconWhite, FlaskIconYellow, "View", "Chemicals");
        AddTabButton(BookIcon, BookIconWhite, BookIconYellow, "Published", "Material");

        SetActiveTab(_window.Tabs.CurrentTab);
        _window.Tabs.OnTabChanged += SetActiveTab;
    }

    private void AddTabButton(string iconPath, string whiteIconPath, string yellowIconPath, string line1, string line2)
    {
        var index = _tabButtons.Count;
        var tab = new TabButtonControl(
            _resourceCache.GetTexture(iconPath),
            _resourceCache.GetTexture(whiteIconPath),
            _resourceCache.GetTexture(yellowIconPath),
            line1,
            line2);
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
        private readonly Texture _blackTexture;
        private readonly Texture _whiteTexture;
        private readonly Texture _yellowTexture;
        private readonly TextureRect _icon;
        private readonly Label _line1;
        private readonly Label _line2;
        private bool _active;
        private bool _hovered;

        public TabButtonControl(Texture blackTexture, Texture whiteTexture, Texture yellowTexture, string line1, string line2)
        {
            _blackTexture = blackTexture;
            _whiteTexture = whiteTexture;
            _yellowTexture = yellowTexture;
            HorizontalExpand = true;
            Modulate = Color.White;
            ModulateSelfOverride = Color.White;
            StyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.Transparent,
                BorderColor = AmberBorder,
                BorderThickness = new Thickness(4),
            };

            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                SeparationOverride = 8,
                Margin = new Thickness(6, 6),
            };

            _icon = new TextureRect
            {
                Texture = yellowTexture,
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                SetSize = new Vector2(24, 24),
                VerticalAlignment = VAlignment.Center,
                ModulateSelfOverride = Color.White,
            };

            var textCol = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                VerticalAlignment = VAlignment.Center,
            };

            _line1 = new Label { Text = line1, HorizontalAlignment = HAlignment.Center };
            _line2 = new Label { Text = line2, HorizontalAlignment = HAlignment.Center };
            textCol.AddChild(_line1);
            textCol.AddChild(_line2);

            row.AddChild(_icon);
            row.AddChild(textCol);
            AddChild(row);

            OnMouseEntered += _ =>
            {
                _hovered = true;
                UpdateIcon();
            };
            OnMouseExited += _ =>
            {
                _hovered = false;
                UpdateIcon();
            };

            SetActive(false);
        }

        private void UpdateIcon()
        {
            _icon.Texture = _hovered ? _whiteTexture : _active ? _blackTexture : _yellowTexture;
        }

        public void SetActive(bool active)
        {
            _active = active;

            var box = (StyleBoxFlat) StyleBoxOverride!;
            box.BackgroundColor = active ? AmberBright : Color.Transparent;

            var fontColor = active ? AmberBlack : AmberBright;
            _line1.FontColorOverride = fontColor;
            _line2.FontColorOverride = fontColor;
            UpdateIcon();
        }
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!TryGetResearch(out var research))
            return;

        var credits = research.Credits;
        var clearance = research.ClearanceLevel;

        _window.ClearanceLabel.Text = $"CLEARANCE LEVEL {clearance}";
        _window.CreditsLabel.Text = $"Credits available: {credits}";

        _window.ReprintContractButton.Disabled = string.IsNullOrEmpty(research.LastPickedContractReagent);

        if (clearance < MaxClearanceLevel)
        {
            var cost = Math.Max(ResearchLevelIncreaseMultiplier * clearance + 1, 1);
            _brokerClearanceLabel!.Text = $"Improve ({cost}CR)";
            _window.BrokerClearanceButton.Disabled = cost > credits;
            SetPurchasableStyle(_window.BrokerClearanceButton, cost <= credits, AmberWashed);
        }
        else if (!research.ReachedXAccess)
        {
            _brokerClearanceLabel!.Text = $"Request 5X Access ({XAccessCost}CR)";
            _window.BrokerClearanceButton.Disabled = XAccessCost > credits;
            SetPurchasableStyle(_window.BrokerClearanceButton, XAccessCost <= credits, AmberWashed);
        }
        else
        {
            _brokerClearanceLabel!.Text = "5X Access Granted";
            _window.BrokerClearanceButton.Disabled = true;
            SetPurchasableStyle(_window.BrokerClearanceButton, false, AmberWashed);
        }

        var duration = research.PickedThisCycle ? TimeSpan.FromMinutes(6) : TimeSpan.FromMinutes(3);
        _window.CycleStart = research.NextReroll - duration;
        _window.CycleEnd = research.NextReroll;

        PopulateContracts(research);
        PopulateDocuments(_window.DocumentsBox, _window.DocumentsEmptyLabel, research, research.Documents, isPublishedTab: false);
        PopulateDocuments(_window.PublishedBox, _window.PublishedEmptyLabel, research, research.Published, isPublishedTab: true);
    }

    private void OnBrokerClearancePressed()
    {
        if (!TryGetResearch(out var research))
            return;

        var cost = Math.Max(ResearchLevelIncreaseMultiplier * research.ClearanceLevel + 1, 1);

        var confirm = new ConfirmationWindow();
        confirm.Setup(
            "Improve Clearance",
            $"Are you sure you want to spend {cost} research credits to increase the clearance immediately?",
            "Confirm",
            "Cancel"
        );
        confirm.AcceptButton.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCResearchComputerBrokerClearanceBuiMsg());
            confirm.Close();
        };
        confirm.DenyButton.OnPressed += _ => confirm.Close();
        confirm.OpenCentered();
    }

    private void OnRequestXAccessPressed()
    {
        var confirm = new ConfirmationWindow();
        confirm.Setup(
            "Request 5X Access",
            $"Are you sure you want to spend {XAccessCost} research credits to request 5X clearance access?",
            "Confirm",
            "Cancel"
        );
        confirm.AcceptButton.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCResearchComputerRequestXAccessBuiMsg());
            confirm.Close();
        };
        confirm.DenyButton.OnPressed += _ => confirm.Close();
        confirm.OpenCentered();
    }

    private bool TryGetResearch(out RMCChemistryResearchComponent research)
    {
        var query = _entity.EntityQueryEnumerator<RMCChemistryResearchComponent>();
        if (query.MoveNext(out _, out var comp))
        {
            research = comp;
            return true;
        }

        research = default!;
        return false;
    }

    private void PopulateContracts(RMCChemistryResearchComponent research)
    {
        var box = _window!.ContractsBox;

        for (var i = 0; i < research.Contracts.Count; i++)
        {
            var slot = research.Contracts[i];

            ContractCard card;
            if (i < box.ChildCount && box.GetChild(i) is ContractCard existing)
            {
                card = existing;
            }
            else
            {
                card = new ContractCard(
                    _resourceCache.GetTexture(PrintIcon),
                    _resourceCache.GetTexture(PrintIconWhite),
                    _resourceCache.GetTexture(PrintIconYellow),
                    _boldFont,
                    _smallFont);
                box.AddChild(card);
            }

            var propertyName = _prototype.TryIndex<ChemGeneratorPropertyPrototype>(slot.PropertyHintId, out var property)
                ? property.ID
                : slot.PropertyHintId;
            var ingredientName = _rmcReagent.TryIndex(slot.IngredientHintId, out var ingredientReagent)
                ? ingredientReagent.LocalizedName
                : slot.IngredientHintId;

            card.NameLabel.Text = slot.Name;
            card.DifficultyLabel.Text = "Difficulty: " + slot.Tier switch
            {
                1 => "Easy",
                2 => "Intermediate",
                _ => "Hard",
            };
            card.RecipeHintLabel.Text = $"Early assessment shows one part of the recipe is {ingredientName}";
            card.PropertyHintLabel.Text = $"Early testing shows property of {propertyName}";
            card.TakeButton.Disabled = slot.Taken || research.PickedThisCycle;
            SetPurchasableStyle(card.TakeButton, !card.TakeButton.Disabled);
            card.SetEnabledVisual(!card.TakeButton.Disabled);
            card.TakeButtonLabel.Text = slot.Taken ? "Contract Taken" : "Take Contract";
            card.SlotIndex = i;
            card.OnTake = index => SendPredictedMessage(new RMCResearchComputerTakeContractBuiMsg(index));
        }

        box.RemoveChildrenAfter(research.Contracts.Count);
    }

    private sealed class ContractCard : BoxContainer
    {
        public readonly Label NameLabel;
        public readonly Label DifficultyLabel;
        public readonly Label RecipeHintLabel;
        public readonly Label PropertyHintLabel;
        public readonly Button TakeButton;
        public readonly Label TakeButtonLabel;

        private readonly TextureRect _takeIcon;
        private readonly Texture _blackIcon;
        private readonly Texture _whiteIcon;
        private readonly Texture _yellowIcon;
        private bool _enabled = true;

        public int SlotIndex;
        public Action<int>? OnTake;

        public ContractCard(Texture printIcon, Texture printIconWhite, Texture printIconYellow, Font boldFont, Font smallFont)
        {
            _blackIcon = printIcon;
            _whiteIcon = printIconWhite;
            _yellowIcon = printIconYellow;
            Orientation = LayoutOrientation.Vertical;
            HorizontalExpand = true;

            var panel = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = AmberBlack, BorderColor = AmberBright, BorderThickness = new Thickness(1) },
            };

            var inner = new BoxContainer { Orientation = LayoutOrientation.Vertical, Margin = new Thickness(6) };

            NameLabel = new Label { FontColorOverride = AmberBright };

            var divider = new PanelContainer
            {
                MinHeight = 1,
                Margin = new Thickness(0, 2, 0, 4),
                PanelOverride = new StyleBoxFlat { BackgroundColor = AmberBright },
            };

            DifficultyLabel = new Label { FontColorOverride = AmberDim };
            RecipeHintLabel = new Label { FontColorOverride = AmberDim, FontOverride = smallFont };
            PropertyHintLabel = new Label { FontColorOverride = AmberDim, FontOverride = smallFont };
            TakeButton = new Button
            {
                HorizontalExpand = true,
                Margin = new Thickness(0, 4, 0, 0),
                Modulate = Color.White,
                ModulateSelfOverride = Color.White,
                StyleBoxOverride = new StyleBoxFlat
                {
                    BackgroundColor = AmberBright,
                    BorderColor = AmberBorder,
                    BorderThickness = new Thickness(2),
                },
            };

            var takeRow = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalAlignment = HAlignment.Left,
                VerticalAlignment = VAlignment.Center,
                SeparationOverride = 8,
                Margin = new Thickness(10, 4),
            };
            _takeIcon = new TextureRect
            {
                Texture = printIcon,
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                SetSize = new Vector2(28, 28),
                VerticalAlignment = VAlignment.Center,
                ModulateSelfOverride = Color.White,
            };
            TakeButtonLabel = new Label { FontColorOverride = AmberBlack, FontOverride = boldFont, VerticalAlignment = VAlignment.Center };
            takeRow.AddChild(_takeIcon);
            takeRow.AddChild(TakeButtonLabel);
            TakeButton.AddChild(takeRow);

            TakeButton.OnMouseEntered += _ => _takeIcon.Texture = _whiteIcon;
            TakeButton.OnMouseExited += _ => _takeIcon.Texture = _enabled ? _blackIcon : _yellowIcon;

            inner.AddChild(NameLabel);
            inner.AddChild(divider);
            inner.AddChild(DifficultyLabel);
            inner.AddChild(RecipeHintLabel);
            inner.AddChild(PropertyHintLabel);
            inner.AddChild(TakeButton);

            panel.AddChild(inner);
            AddChild(panel);

            TakeButton.OnPressed += _ => OnTake?.Invoke(SlotIndex);
        }

        public void SetEnabledVisual(bool enabled)
        {
            _enabled = enabled;
            _takeIcon.Texture = enabled ? _blackIcon : _yellowIcon;
            TakeButtonLabel.FontColorOverride = enabled ? AmberBlack : AmberBright;
        }
    }

    private void PopulateDocuments(BoxContainer box, Label emptyLabel, RMCChemistryResearchComponent research, Dictionary<string, List<DocumentEntry>> documents, bool isPublishedTab)
    {
        var sortColumn = isPublishedTab ? _publishedSort : _documentsSort;
        var sortAscending = isPublishedTab ? _publishedSortAscending : _documentsSortAscending;
        var timeButton = isPublishedTab ? _window!.PublishedSortTimeButton : _window!.DocumentsSortTimeButton;
        var compoundButton = isPublishedTab ? _window!.PublishedSortCompoundButton : _window!.DocumentsSortCompoundButton;
        UpdateSortButtonText(timeButton, compoundButton, isPublishedTab ? "Published" : "Scan Time", sortColumn, sortAscending);

        var hideOld = _window!.HideOldReportsButton.Pressed;

        var publishedKeys = research.Published
            .SelectMany(kv => kv.Value.Select(e => (Category: kv.Key, Compound: GetCompoundName(e.Title))))
            .ToHashSet();

        var flattened = ApplySort(
            documents.SelectMany(kv => kv.Value.Select(entry => (Category: kv.Key, Entry: entry))),
            sortColumn,
            sortAscending).ToList();

        if (hideOld)
        {
            var seen = new HashSet<string>();
            flattened = flattened.Where(x => seen.Add(x.Entry.Title)).ToList();
        }

        emptyLabel.Visible = flattened.Count == 0;

        var index = 0;
        foreach (var (category, entry) in flattened)
        {
            DocumentRow row;
            if (index < box.ChildCount && box.GetChild(index) is DocumentRow existing)
            {
                row = existing;
            }
            else
            {
                row = new DocumentRow(
                    _resourceCache.GetTexture(BookIcon), _resourceCache.GetTexture(BookIconWhite),
                    _resourceCache.GetTexture(PrintIcon), _resourceCache.GetTexture(PrintIconWhite),
                    _resourceCache.GetTexture(AnnounceIcon), _resourceCache.GetTexture(AnnounceIconWhite));
                box.AddChild(row);
            }

            var rowPublished = isPublishedTab || publishedKeys.Contains((category, GetCompoundName(entry.Title)));

            row.ScanTimeLabel.Text = entry.Time.ToString(@"hh\:mm\:ss");
            row.TypeLabel.Text = category;
            row.CompoundLabel.Text = GetCompoundName(entry.Title);
            row.Category = category;
            row.Title = entry.Title;
            row.Published = rowPublished;
            row.PublishButton.Text = rowPublished ? "Unpublish" : "Publish";
            row.OnRead = (category2, title2, published2) => SendPredictedMessage(new RMCResearchComputerReadDocumentBuiMsg(category2, title2, published2));
            row.OnPrint = (category2, title2, published2) => SendPredictedMessage(new RMCResearchComputerPrintDocumentBuiMsg(category2, title2, published2));
            row.OnPublishToggle = (category2, title2, published2) => SendPredictedMessage(published2
                ? new RMCResearchComputerUnpublishDocumentBuiMsg(category2, title2)
                : new RMCResearchComputerPublishDocumentBuiMsg(category2, title2));
            row.OnAnnounce = (category2, title2) => SendPredictedMessage(new RMCResearchComputerAnnounceDocumentBuiMsg(category2, title2));

            index++;
        }

        box.RemoveChildrenAfter(index);
    }

    private static string GetCompoundName(string title)
    {
        var dashIndex = title.IndexOf(" - ", StringComparison.Ordinal);
        return dashIndex < 0 ? title : title[(dashIndex + 3)..];
    }

    private const int ScanTimeColumnWidth = 100;
    private const int TypeColumnWidth = 140;

    private sealed class DocumentRow : BoxContainer
    {
        public readonly Label ScanTimeLabel;
        public readonly Label TypeLabel;
        public readonly Label CompoundLabel;
        public readonly Button ReadButton;
        public readonly Button PrintButton;
        public readonly Button AnnounceButton;
        public readonly Button PublishButton;

        public string Category = string.Empty;
        public string Title = string.Empty;
        public bool Published;
        public Action<string, string, bool>? OnRead;
        public Action<string, string, bool>? OnPrint;
        public Action<string, string, bool>? OnPublishToggle;
        public Action<string, string>? OnAnnounce;

        public DocumentRow(
            Texture bookIcon, Texture bookIconWhite,
            Texture printIcon, Texture printIconWhite,
            Texture announceIcon, Texture announceIconWhite)
        {
            Orientation = LayoutOrientation.Horizontal;
            HorizontalExpand = true;
            SeparationOverride = 4;
            Margin = new Thickness(0, 0, 0, 2);

            ScanTimeLabel = new Label { MinWidth = ScanTimeColumnWidth, FontColorOverride = AmberDim };
            TypeLabel = new Label { MinWidth = TypeColumnWidth, FontColorOverride = AmberDim };
            CompoundLabel = new Label { HorizontalExpand = true, FontColorOverride = AmberDim };
            ReadButton = new Button { MinWidth = 90 };
            PrintButton = new Button { MinWidth = 90 };
            AnnounceButton = new Button { MinWidth = 90 };
            PublishButton = new Button { Text = "Publish", MinWidth = 80 };

            foreach (var button in new[] { ReadButton, PrintButton, AnnounceButton, PublishButton })
            {
                button.Modulate = Color.White;
                button.ModulateSelfOverride = Color.White;
                button.StyleBoxOverride = new StyleBoxFlat
                {
                    BackgroundColor = AmberBright,
                    BorderColor = AmberBorder,
                    BorderThickness = new Thickness(2),
                };
                button.Label.FontColorOverride = AmberBlack;
            }

            IconButton(ReadButton, bookIcon, bookIconWhite, "Read");
            IconButton(PrintButton, printIcon, printIconWhite, "Print");
            IconButton(AnnounceButton, announceIcon, announceIconWhite, "Announce");

            AddChild(ScanTimeLabel);
            AddChild(TypeLabel);
            AddChild(CompoundLabel);
            AddChild(ReadButton);
            AddChild(PrintButton);
            AddChild(AnnounceButton);
            AddChild(PublishButton);

            ReadButton.OnPressed += _ => OnRead?.Invoke(Category, Title, Published);
            PrintButton.OnPressed += _ => OnPrint?.Invoke(Category, Title, Published);
            AnnounceButton.OnPressed += _ => OnAnnounce?.Invoke(Category, Title);
            PublishButton.OnPressed += _ => OnPublishToggle?.Invoke(Category, Title, Published);
        }

        private static void IconButton(Button button, Texture texture, Texture whiteTexture, string text)
        {
            button.Text = null;

            var row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                SeparationOverride = 4,
                Margin = new Thickness(4, 2),
            };

            var icon = new TextureRect
            {
                Texture = texture,
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                SetSize = new Vector2(16, 16),
                VerticalAlignment = VAlignment.Center,
                ModulateSelfOverride = Color.White,
            };

            var label = new Label { Text = text, FontColorOverride = AmberBlack, VerticalAlignment = VAlignment.Center };

            row.AddChild(icon);
            row.AddChild(label);
            button.AddChild(row);

            button.OnMouseEntered += _ => icon.Texture = whiteTexture;
            button.OnMouseExited += _ => icon.Texture = texture;
        }
    }

    private Label Icon(Button button, string texturePath, string whiteTexturePath, string text)
    {
        button.Text = null;

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = Control.HAlignment.Stretch,
            VerticalAlignment = Control.VAlignment.Center,
            Margin = new Thickness(10, 0, 6, 0),
            SeparationOverride = 8,
        };

        var texture = new TextureRect
        {
            Texture = _resourceCache.GetTexture(texturePath),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(16, 16),
            VerticalAlignment = Control.VAlignment.Center,
        };
        row.AddChild(texture);

        var label = new Label
        {
            Text = text,
            FontColorOverride = Color.Black,
            VerticalAlignment = Control.VAlignment.Center,
            HorizontalExpand = true,
            ClipText = true,
        };
        row.AddChild(label);

        button.AddChild(row);

        button.OnMouseEntered += _ => texture.Texture = _resourceCache.GetTexture(whiteTexturePath);
        button.OnMouseExited += _ => texture.Texture = _resourceCache.GetTexture(texturePath);

        return label;
    }
}
