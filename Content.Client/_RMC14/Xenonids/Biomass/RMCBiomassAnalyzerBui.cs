using System.Linq;
using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Content.Client.Resources;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Xenonids.Biomass;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Biomass;

[UsedImplicitly]
public sealed class RMCBiomassAnalyzerBui : BoundUserInterface, IRefreshableBui
{
    private const string EjectIcon = "/Textures/_RMC14/Interface/Icons/eject.svg.192dpi.png";
    private const string EjectIconWhite = "/Textures/_RMC14/Interface/Icons/eject-white.svg.192dpi.png";
    private const string FlaskIcon = "/Textures/_RMC14/Interface/Icons/flask.svg.192dpi.png";
    private const string FlaskIconWhite = "/Textures/_RMC14/Interface/Icons/flask-white.svg.192dpi.png";
    private const string PlayIcon = "/Textures/_RMC14/Interface/Icons/play.svg.192dpi.png";
    private const string PlayIconWhite = "/Textures/_RMC14/Interface/Icons/play-white.svg.192dpi.png";
    private const string StopIcon = "/Textures/_RMC14/Interface/Icons/stop.svg.192dpi.png";
    private const string StopIconWhite = "/Textures/_RMC14/Interface/Icons/stop-white.svg.192dpi.png";
    private const string PrintIcon = "/Textures/_RMC14/Interface/Icons/print.svg.192dpi.png";
    private const string PrintIconWhite = "/Textures/_RMC14/Interface/Icons/print-white.svg.192dpi.png";
    private const string CancelIcon = "/Textures/_RMC14/Interface/Icons/square-minus.svg.192dpi.png";
    private const string CancelIconWhite = "/Textures/_RMC14/Interface/Icons/square-minus-white.svg.192dpi.png";

    private static readonly Color Amber = Color.FromHex("#FAC000");
    private static readonly Color AmberDim = Color.FromHex("#8A6800");
    private static readonly Color AmberBlack = Color.FromHex("#221801");

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private RMCBiomassAnalyzerWindow? _window;
    private RMCBiomassUpgradeCategory _selectedCategory = RMCBiomassUpgradeCategory.Machinery;
    private IconHandle? _queueToggleIcon;
    private IconHandle? _autoProcessIcon;

    public RMCBiomassAnalyzerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCBiomassAnalyzerWindow>();

        BuildCategoryBox();

        Icon(_window.EjectOrganButton, EjectIcon, EjectIconWhite, "Eject Biomass");
        _autoProcessIcon = IconHandled(_window.ProcessOrganButton, FlaskIcon, FlaskIconWhite, "Auto: OFF");
        _queueToggleIcon = IconHandled(_window.ToggleQueueButton, PlayIcon, PlayIconWhite, "START");

        _window.EjectOrganButton.OnPressed += _ => SendPredictedMessage(new RMCBiomassAnalyzerEjectOrganBuiMsg());
        _window.ProcessOrganButton.OnPressed += _ => SendPredictedMessage(new RMCBiomassAnalyzerToggleAutoProcessBuiMsg());
        _window.ToggleQueueButton.OnPressed += _ => SendPredictedMessage(new RMCBiomassAnalyzerToggleQueueBuiMsg());

        _window.QueueExpandButton.OnPressed += _ =>
        {
            _window.QueueContent.Visible = !_window.QueueContent.Visible;
            _window.QueueExpandButton.Text = _window.QueueContent.Visible ? "▼" : "▶";
        };

        Refresh();
    }

    private void BuildCategoryBox()
    {
        _window!.CategoryBox.Clear();

        var categories = Enum.GetValues<RMCBiomassUpgradeCategory>();
        foreach (var category in categories)
        {
            _window.CategoryBox.AddItem(category.ToString(), (int) category);
        }

        _window.CategoryBox.SelectId((int) _selectedCategory);
        _window.CategoryBox.OnItemSelected += args =>
        {
            _window.CategoryBox.SelectId(args.Id);
            _selectedCategory = (RMCBiomassUpgradeCategory) args.Id;
            Refresh();
        };
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

    private static int GetCurrentCost(RMCBiomassUpgradePrototype upgrade, RMCBiomassAnalyzerComponent analyzer)
    {
        var count = analyzer.PurchaseCounts.GetValueOrDefault(upgrade.ID);
        return Math.Clamp(upgrade.Cost + upgrade.PriceChange * count, upgrade.MinimumPrice, upgrade.MaximumPrice);
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!TryGetAnalyzer(out var analyzer))
            return;

        if (!_entity.TryGetComponent(Owner, out RMCBiomassAnalyzerMachineComponent? machine))
            return;

        var clearance = GetClearanceLevel();

        _window.PointsLabel.Text = $"Biological Matter: {analyzer.Points}";

        var busy = machine.PrintingUntil != null;
        _window.EjectOrganButton.Disabled = machine.HeldOrgan == null || busy;
        _window.OrganStatusLabel.Text = machine.HeldOrgan == null
            ? "Receptacle is empty, analyzing is impossible!"
            : "Biomass accepted. Ready to analyze.";
        _window.OrganStatusLabel.FontColorOverride = machine.HeldOrgan == null ? AmberDim : Amber;

        if (_autoProcessIcon != null)
            _autoProcessIcon.Label.Text = machine.AutoProcessOrgan ? "Auto: ON" : "Auto: OFF";

        PopulateQueue(machine);
        PopulateCategory(analyzer, clearance);
    }

    private void PopulateQueue(RMCBiomassAnalyzerMachineComponent machine)
    {
        _queueToggleIcon?.SetPath(machine.QueueProcessing ? StopIcon : PlayIcon, machine.QueueProcessing ? StopIconWhite : PlayIconWhite);
        if (_queueToggleIcon != null)
            _queueToggleIcon.Label.Text = machine.QueueProcessing ? "STOP" : "START";

        _window!.QueueEmptyLabel.Visible = machine.PrintQueue.Count == 0;

        var index = 0;
        foreach (var upgradeId in machine.PrintQueue)
        {
            QueueEntryControl entry;
            if (index < _window.QueueBox.ChildCount && _window.QueueBox.GetChild(index) is QueueEntryControl existing)
            {
                entry = existing;
            }
            else
            {
                entry = new QueueEntryControl();
                _window.QueueBox.AddChild(entry);
            }

            entry.Index = index;
            entry.NameLabel.Text = _prototype.TryIndex<RMCBiomassUpgradePrototype>(upgradeId, out var upgrade)
                ? upgrade.Name
                : upgradeId;
            entry.OnCancel = i => SendPredictedMessage(new RMCBiomassAnalyzerRemoveFromQueueBuiMsg(i));

            index++;
        }

        _window.QueueBox.RemoveChildrenAfter(index);
    }

    private void PopulateCategory(RMCBiomassAnalyzerComponent analyzer, int clearance)
    {
        var box = _window!.UpgradesBox;
        var upgrades = _prototype.EnumeratePrototypes<RMCBiomassUpgradePrototype>()
            .Where(u => u.Category == _selectedCategory)
            .OrderBy(u => u.RequiredClearance)
            .ThenBy(u => u.Cost)
            .ToList();

        var index = 0;
        foreach (var upgrade in upgrades)
        {
            UpgradeRow row;
            if (index < box.ChildCount && box.GetChild(index) is UpgradeRow existing)
            {
                row = existing;
            }
            else
            {
                row = new UpgradeRow();
                box.AddChild(row);
            }

            var cost = GetCurrentCost(upgrade, analyzer);
            var clearanceMet = clearance >= upgrade.RequiredClearance;

            row.NameLabel.Text = upgrade.Name;
            row.ClearanceLabel.Text = $"Clearance {upgrade.RequiredClearance} Required";
            row.TrendLabel.Text = upgrade.PriceChange switch
            {
                0 => "This technology will not change its value with purchase.",
                > 0 => $"This technology will increase in cost by {upgrade.PriceChange} each purchase.",
                _ => $"This technology will decrease in cost by {-upgrade.PriceChange} each purchase.",
            };

            row.PrintButtonLabel.Text = $"Print ({cost})";

            row.PrintButton.Disabled = !clearanceMet;
            row.Print2Button.Disabled = !clearanceMet;
            row.Print5Button.Disabled = !clearanceMet;
            row.Print10Button.Disabled = !clearanceMet;

            row.UpgradeId = upgrade.ID;
            row.OnPrint = (id, amount) => SendPredictedMessage(new RMCBiomassAnalyzerEnqueuePrintBuiMsg(id, amount));

            index++;
        }

        box.RemoveChildrenAfter(index);
    }

    private static Label Icon(Button button, string texturePath, string whiteTexturePath, string text)
    {
        return IconHandled(button, texturePath, whiteTexturePath, text).Label;
    }

    private static IconHandle IconHandled(Button button, string texturePath, string whiteTexturePath, string text)
    {
        var resourceCache = IoCManager.Resolve<IResourceCache>();
        button.Text = null;

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = Control.HAlignment.Center,
            VerticalAlignment = Control.VAlignment.Center,
            SeparationOverride = 6,
        };

        var texture = new TextureRect
        {
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(14, 14),
            VerticalAlignment = Control.VAlignment.Center,
        };
        row.AddChild(texture);

        var label = new Label
        {
            Text = text,
            FontColorOverride = AmberBlack,
            VerticalAlignment = Control.VAlignment.Center,
        };
        row.AddChild(label);

        button.AddChild(row);

        var handle = new IconHandle(resourceCache, texture, label);
        handle.SetPath(texturePath, whiteTexturePath);

        button.OnMouseEntered += _ => handle.SetHovered(true);
        button.OnMouseExited += _ => handle.SetHovered(false);

        return handle;
    }

    private sealed class IconHandle
    {
        private readonly IResourceCache _resourceCache;
        private readonly TextureRect _texture;
        public readonly Label Label;

        private string _path = "";
        private string _whitePath = "";
        private bool _hovered;

        public IconHandle(IResourceCache resourceCache, TextureRect texture, Label label)
        {
            _resourceCache = resourceCache;
            _texture = texture;
            Label = label;
        }

        public void SetPath(string path, string whitePath)
        {
            _path = path;
            _whitePath = whitePath;
            Apply();
        }

        public void SetHovered(bool hovered)
        {
            _hovered = hovered;
            Apply();
        }

        private void Apply()
        {
            _texture.Texture = _resourceCache.GetTexture(_hovered ? _whitePath : _path);
        }
    }

    private sealed class QueueEntryControl : BoxContainer
    {
        public readonly Label NameLabel;
        public readonly Button CancelButton;

        public int Index;
        public Action<int>? OnCancel;

        public QueueEntryControl()
        {
            Orientation = LayoutOrientation.Horizontal;
            HorizontalExpand = true;

            NameLabel = new Label { HorizontalExpand = true, FontColorOverride = Amber };
            CancelButton = new Button
            {
                MinSize = new Vector2(90, 24),
                Modulate = Color.White,
                ModulateSelfOverride = Color.White,
                StyleBoxOverride = new StyleBoxFlat { BackgroundColor = AmberBlack, BorderColor = AmberDim, BorderThickness = new Thickness(2) },
            };
            Icon(CancelButton, CancelIcon, CancelIconWhite, "Cancel");

            AddChild(NameLabel);
            AddChild(CancelButton);

            CancelButton.OnPressed += _ => OnCancel?.Invoke(Index);
        }
    }

    private sealed class UpgradeRow : PanelContainer
    {
        public readonly Label NameLabel;
        public readonly Label ClearanceLabel;
        public readonly Label TrendLabel;
        public readonly Button PrintButton;
        public readonly Label PrintButtonLabel;
        public readonly Button Print2Button;
        public readonly Button Print5Button;
        public readonly Button Print10Button;

        public string UpgradeId = string.Empty;
        public Action<string, int>? OnPrint;

        public UpgradeRow()
        {
            HorizontalExpand = true;
            PanelOverride = new StyleBoxFlat { BackgroundColor = AmberBlack, BorderColor = AmberDim, BorderThickness = new Thickness(0, 0, 0, 1) };

            var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, Margin = new Thickness(4), SeparationOverride = 4 };

            NameLabel = new Label { MinWidth = 200, FontColorOverride = Amber };

            var detailsBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true };
            ClearanceLabel = new Label { FontColorOverride = Amber };
            TrendLabel = new Label { FontColorOverride = AmberDim };
            detailsBox.AddChild(ClearanceLabel);
            detailsBox.AddChild(TrendLabel);

            var printColumn = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, MinWidth = 140 };

            PrintButton = new Button
            {
                HorizontalExpand = true,
                Modulate = Color.White,
                ModulateSelfOverride = Color.White,
                StyleBoxOverride = new StyleBoxFlat { BackgroundColor = Amber, BorderColor = AmberDim, BorderThickness = new Thickness(2) },
            };
            PrintButtonLabel = Icon(PrintButton, PrintIcon, PrintIconWhite, "Print");

            var bulkRow = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, Margin = new Thickness(0, 2, 0, 0), SeparationOverride = 2 };
            Print2Button = MakeBulkButton("x2");
            Print5Button = MakeBulkButton("x5");
            Print10Button = MakeBulkButton("x10");
            bulkRow.AddChild(Print2Button);
            bulkRow.AddChild(Print5Button);
            bulkRow.AddChild(Print10Button);

            printColumn.AddChild(PrintButton);
            printColumn.AddChild(bulkRow);

            row.AddChild(NameLabel);
            row.AddChild(MakeDivider());
            row.AddChild(detailsBox);
            row.AddChild(MakeDivider());
            row.AddChild(printColumn);

            AddChild(row);

            PrintButton.OnPressed += _ => OnPrint?.Invoke(UpgradeId, 1);
            Print2Button.OnPressed += _ => OnPrint?.Invoke(UpgradeId, 2);
            Print5Button.OnPressed += _ => OnPrint?.Invoke(UpgradeId, 5);
            Print10Button.OnPressed += _ => OnPrint?.Invoke(UpgradeId, 10);
        }

        private static Control MakeDivider()
        {
            return new PanelContainer
            {
                MinWidth = 1,
                VerticalExpand = true,
                PanelOverride = new StyleBoxFlat { BackgroundColor = AmberDim },
            };
        }

        private static Button MakeBulkButton(string text)
        {
            var button = new Button
            {
                Text = text,
                HorizontalExpand = true,
                Modulate = Color.White,
                ModulateSelfOverride = Color.White,
                StyleBoxOverride = new StyleBoxFlat { BackgroundColor = Amber, BorderColor = AmberDim, BorderThickness = new Thickness(2) },
            };
            button.Label.FontColorOverride = AmberBlack;
            return button;
        }
    }
}
