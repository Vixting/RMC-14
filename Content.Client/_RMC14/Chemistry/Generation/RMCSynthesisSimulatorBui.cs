using System.Linq;
using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Content.Client.Resources;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Chemistry.Generation;

[UsedImplicitly]
public sealed class RMCSynthesisSimulatorBui : BoundUserInterface, IRefreshableBui
{
    private const string RadiationIcon = "/Textures/_RMC14/Interface/Icons/radiation.svg.192dpi.png";
    private const string RadiationIconWhite = "/Textures/_RMC14/Interface/Icons/radiation-white.svg.192dpi.png";
    private const string StopIcon = "/Textures/_RMC14/Interface/Icons/stop.svg.192dpi.png";
    private const string StopIconWhite = "/Textures/_RMC14/Interface/Icons/stop-white.svg.192dpi.png";
    private const string EjectIcon = "/Textures/_RMC14/Interface/Icons/eject.svg.192dpi.png";
    private const string EjectIconWhite = "/Textures/_RMC14/Interface/Icons/eject-white.svg.192dpi.png";
    private const string SquarePlusIcon = "/Textures/_RMC14/Interface/Icons/square-plus.svg.192dpi.png";
    private const string SquarePlusIconWhite = "/Textures/_RMC14/Interface/Icons/square-plus-white.svg.192dpi.png";
    private const string SquareMinusIcon = "/Textures/_RMC14/Interface/Icons/square-minus.svg.192dpi.png";
    private const string SquareMinusIconWhite = "/Textures/_RMC14/Interface/Icons/square-minus-white.svg.192dpi.png";
    private const string ArrowRightIcon = "/Textures/_RMC14/Interface/Icons/arrow-right-long.svg.192dpi.png";
    private const string ArrowRightIconWhite = "/Textures/_RMC14/Interface/Icons/arrow-right-long-white.svg.192dpi.png";
    private const string RepeatIcon = "/Textures/_RMC14/Interface/Icons/arrows-rotate.svg.192dpi.png";
    private const string RepeatIconWhite = "/Textures/_RMC14/Interface/Icons/arrows-rotate-white.svg.192dpi.png";
    private const string ServerIcon = "/Textures/_RMC14/Interface/Icons/server.svg.192dpi.png";
    private const string ServerIconWhite = "/Textures/_RMC14/Interface/Icons/server-white.svg.192dpi.png";

    private static readonly Color ModeColor = Color.FromHex("#d3bd9a");
    private static readonly Color ActionColor = Color.FromHex("#d6d6d6");
    private static readonly Color OverrideActiveColor = Color.FromHex("#b08080");
    private static readonly Color CreditsGood = Color.FromHex("#9dc79f");
    private static readonly Color CreditsAverage = Color.FromHex("#d3bd9a");
    private static readonly Color CreditsBad = Color.FromHex("#b08080");

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SharedContainerSystem _container;
    private readonly SharedRMCSynthesisSimulatorSystem _simulator;
    private readonly RMCReagentSystem _rmcReagent;
    private readonly IResourceCache _resourceCache;
    private readonly Font _boldFont;

    private RMCSynthesisSimulatorWindow? _window;
    private bool _overrideSafety;
    private int? _selectedRecipeIndex;

    public RMCSynthesisSimulatorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _container = _entity.System<SharedContainerSystem>();
        _simulator = _entity.System<SharedRMCSynthesisSimulatorSystem>();
        _rmcReagent = _entity.System<RMCReagentSystem>();
        _resourceCache = IoCManager.Resolve<IResourceCache>();
        _boldFont = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-Bold.ttf", 12);
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCSynthesisSimulatorWindow>();
        _window.CreditsLabel.FontOverride = _boldFont;

        foreach (var modeButton in new[] { _window.AmplifyButton, _window.SuppressButton, _window.RelateButton, _window.AddButton })
        {
            modeButton.ToggleMode = true;
            Colored(modeButton, ModeColor);
        }

        foreach (var actionButton in new[] { _window.SimulateButton, _window.CancelButton, _window.EjectTargetButton, _window.EjectReferenceButton })
        {
            Colored(actionButton, ActionColor);
        }

        _window.OverrideButton.ToggleMode = true;
        Colored(_window.OverrideButton, ActionColor);

        Icon(_window.SimulateButton, RadiationIcon, RadiationIconWhite, "SIMULATE");
        Icon(_window.CancelButton, StopIcon, StopIconWhite, "CANCEL");
        Icon(_window.EjectTargetButton, EjectIcon, EjectIconWhite, "EJECT TARGET");
        Icon(_window.EjectReferenceButton, EjectIcon, EjectIconWhite, "EJECT REFERENCE");
        Icon(_window.AmplifyButton, SquarePlusIcon, SquarePlusIconWhite, "AMPLIFY");
        Icon(_window.SuppressButton, SquareMinusIcon, SquareMinusIconWhite, "SUPPRESS");
        Icon(_window.AddButton, ArrowRightIcon, ArrowRightIconWhite, "ADD");
        Icon(_window.RelateButton, RepeatIcon, RepeatIconWhite, "RELATE");
        Icon(_window.OverrideButton, ServerIcon, ServerIconWhite, "OVERRIDE");

        _window.AmplifyButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorSetModeBuiMsg(SynthesisMode.Amplify));
        _window.SuppressButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorSetModeBuiMsg(SynthesisMode.Suppress));
        _window.RelateButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorSetModeBuiMsg(SynthesisMode.Relate));
        _window.AddButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorSetModeBuiMsg(SynthesisMode.Add));

        _window.EjectTargetButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorEjectTargetBuiMsg());
        _window.EjectReferenceButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorEjectReferenceBuiMsg());
        _window.SimulateButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorSimulateBuiMsg());
        _window.CancelButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorCancelSimulationBuiMsg());
        _window.OverrideButton.OnPressed += _ =>
        {
            _overrideSafety = _window.OverrideButton.Pressed;
            Colored(_window.OverrideButton, _overrideSafety ? OverrideActiveColor : ActionColor);
            Refresh();
        };
        _window.FinalizeButton.OnPressed += _ =>
        {
            if (_selectedRecipeIndex is { } index)
                SendPredictedMessage(new RMCSynthesisSimulatorPickRecipeBuiMsg(index));
        };

        Refresh();
    }

    private static void Colored(Button button, Color color)
    {
        button.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = color,
            BorderColor = Color.Black,
            BorderThickness = new Thickness(1),
        };
        button.ModulateSelfOverride = Color.White;
    }

    private TextureRect Icon(Button button, string texturePath, string whiteTexturePath, string text)
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
            SetSize = new Vector2(20, 20),
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

        return texture;
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true } || !_entity.TryGetComponent(Owner, out RMCSynthesisSimulatorComponent? comp))
            return;

        var hasResearch = TryGetResearch(out var research);
        var credits = hasResearch ? research.Credits : 0;
        var clearance = hasResearch ? research.ClearanceLevel : 1;

        _window.CreditsLabel.Text = $"RESEARCH CREDITS: {credits}";
        _window.ClearanceLabel.Text = $"CLEARANCE: {clearance}";

        _window.CreditsBar.MaxValue = 100;
        _window.CreditsBar.Value = Math.Clamp(credits, 0, 100);
        _window.CreditsBar.ForegroundStyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = credits >= 60 ? CreditsGood : credits >= 15 ? CreditsAverage : CreditsBad,
        };

        var locked = comp.Simulating || comp.Picking;
        var canCancel = locked;
        _window.SimulateButton.Visible = !canCancel;
        _window.CancelButton.Visible = canCancel;
        _window.SimulateButton.Disabled = locked;

        _window.AmplifyButton.Disabled = locked;
        _window.SuppressButton.Disabled = locked;
        _window.RelateButton.Disabled = locked;
        _window.AddButton.Disabled = locked;

        var targetItem = GetSlotItem(comp.TargetSlotId);
        var referenceItem = GetSlotItem(comp.ReferenceSlotId);

        var canEjectTarget = targetItem != null && !locked;
        var canEjectReference = referenceItem != null && !locked;
        _window.EjectTargetButton.Disabled = !canEjectTarget;
        _window.EjectReferenceButton.Disabled = !canEjectReference;

        HighlightMode(comp.Mode);

        var targetName = targetItem is { } t && _entity.TryGetComponent(t, out RMCChemResearchReportComponent? targetReport)
            ? GetReportReagentName(targetReport)
            : null;

        var referenceName = referenceItem is { } r && _entity.TryGetComponent(r, out RMCChemResearchReportComponent? referenceReport)
            ? GetReportReagentName(referenceReport)
            : null;

        _window.TargetNameLabel.Text = $"TARGET NAME: {targetName ?? "CHEMICAL DATA NOT INSERTED"}";
        _window.ReferenceNameLabel.Text = $"REFERENCE NAME: {referenceName ?? "CHEMICAL DATA NOT INSERTED"}";

        var cost = _simulator.GetCost((Owner, comp));
        _window.CostLabel.Text = $"ESTIMATED SIMULATING COST: {(cost > 0 ? cost.ToString() : "NULL")}";

        _window.OverdoseLabel.Text = "OVERDOSE LEVEL AFTER SIMULATION:";

        _window.StatusLabel.Text = "STATUS: " + (comp.Simulating ? "SIMULATING"
            : comp.Picking ? "ANALYSIS READY - PICK A RECIPE"
            : targetItem is null ? "NO TARGET INSERTED"
            : "READY");

        var showReference = comp.Mode is SynthesisMode.Relate or SynthesisMode.Add;

        _window.TargetPropertiesPanel.Visible = targetItem != null;
        _window.ReferencePropertiesPanel.Visible = referenceItem != null && showReference;

        var targetSelectable = comp.Mode != SynthesisMode.Add;
        PopulateProperties(_window.TargetPropertiesBox, targetItem, comp.TargetProperty?.Id, comp.ReferenceProperty?.Id,
            targetSelectable, "This property conflicts with the selected reference property!", id =>
                SendPredictedMessage(new RMCSynthesisSimulatorSelectTargetPropertyBuiMsg(id)));

        if (showReference)
        {
            PopulateProperties(_window.ReferencePropertiesBox, referenceItem, comp.ReferenceProperty?.Id, comp.TargetProperty?.Id,
                true, "This property conflicts with the selected target property!", id =>
                    SendPredictedMessage(new RMCSynthesisSimulatorSelectReferencePropertyBuiMsg(id)));
        }

        _window.RecipePickerPanel.Visible = comp.Picking;
        if (comp.Picking && comp.RecipeCandidates is { } candidates)
            PopulateRecipeCandidates(candidates);
        else
            _selectedRecipeIndex = null;

        _window.FinalizeButton.Disabled = _selectedRecipeIndex is null;
    }

    private void HighlightMode(SynthesisMode mode)
    {
        _window!.AmplifyButton.Pressed = mode == SynthesisMode.Amplify;
        _window.SuppressButton.Pressed = mode == SynthesisMode.Suppress;
        _window.RelateButton.Pressed = mode == SynthesisMode.Relate;
        _window.AddButton.Pressed = mode == SynthesisMode.Add;
    }

    private string GetReportReagentName(RMCChemResearchReportComponent report)
    {
        if (!report.Completed)
            return "ANALYSIS INCOMPLETE";

        return _rmcReagent.TryIndex(report.ReagentId, out var reagent) ? reagent.LocalizedName : report.ReagentId.Id;
    }

    private EntityUid? GetSlotItem(string slotId)
    {
        if (!_container.TryGetContainer(Owner, slotId, out var container))
            return null;

        return container.ContainedEntities.Count > 0 ? container.ContainedEntities[0] : null;
    }

    private void PopulateProperties(
        BoxContainer box,
        EntityUid? item,
        string? selectedId,
        string? opposingSelectedId,
        bool selectable,
        string conflictTooltip,
        Action<string?> onSelect)
    {
        if (item is not { } itemUid || !_entity.TryGetComponent(itemUid, out RMCChemResearchReportComponent? report))
        {
            box.RemoveAllChildren();
            return;
        }

        if (!report.Completed)
        {
            box.RemoveAllChildren();
            box.AddChild(new Label { Text = "Analysis incomplete", FontColorOverride = Color.Red });
            return;
        }

        var index = 0;
        foreach (var p in report.Properties)
        {
            var name = p.PropertyId;
            var description = string.Empty;
            if (_prototype.TryIndex<ChemGeneratorPropertyPrototype>(p.PropertyId, out var proto))
            {
                name = proto.ID;
                description = proto.Description;
            }

            var conflicting = opposingSelectedId != null && ChemPropertyRelations.AreConflicting(p.PropertyId, opposingSelectedId);

            PropertyButton button;
            if (index < box.ChildCount && box.GetChild(index) is PropertyButton existing)
            {
                button = existing;
            }
            else
            {
                button = new PropertyButton { HorizontalExpand = true };
                box.AddChild(button);
            }

            button.Text = $"{name} (lvl {p.Level})";
            button.ToggleMode = selectable;
            button.Pressed = p.PropertyId == selectedId;
            button.Disabled = selectable && conflicting && !_overrideSafety;
            button.ToolTip = conflicting && !_overrideSafety ? conflictTooltip : description;
            button.MouseFilter = selectable ? Control.MouseFilterMode.Stop : Control.MouseFilterMode.Ignore;
            button.PropertyId = p.PropertyId;
            button.OnSelect = selectable ? onSelect : null;

            index++;
        }

        box.RemoveChildrenAfter(index);
    }

    private sealed class PropertyButton : Button
    {
        public string PropertyId = string.Empty;
        public Action<string?>? OnSelect;

        public PropertyButton()
        {
            OnPressed += _ => OnSelect?.Invoke(Pressed ? PropertyId : null);
        }
    }

    private void PopulateRecipeCandidates(List<List<RecipeCandidateIngredient>> candidates)
    {
        var box = _window!.RecipeCandidatesBox;

        for (var i = 0; i < candidates.Count; i++)
        {
            var text = string.Join(", ", candidates[i].Select(c =>
            {
                var name = _rmcReagent.TryIndex(c.Id, out var reagent) ? reagent.LocalizedName : c.Id;
                return c.Catalyst ? $"{name} x{c.Amount} (catalyst)" : $"{name} x{c.Amount}";
            }));

            RecipeButton button;
            if (i < box.ChildCount && box.GetChild(i) is RecipeButton existing)
            {
                button = existing;
            }
            else
            {
                button = new RecipeButton { HorizontalExpand = true };
                box.AddChild(button);
            }

            button.Text = text;
            button.ToggleMode = true;
            button.Pressed = _selectedRecipeIndex == i;
            button.Index = i;
            button.OnSelect = index =>
            {
                _selectedRecipeIndex = index;
                Refresh();
            };
        }

        box.RemoveChildrenAfter(candidates.Count);
    }

    private sealed class RecipeButton : Button
    {
        public int Index;
        public Action<int?>? OnSelect;

        public RecipeButton()
        {
            OnPressed += _ => OnSelect?.Invoke(Pressed ? Index : null);
        }
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
}
