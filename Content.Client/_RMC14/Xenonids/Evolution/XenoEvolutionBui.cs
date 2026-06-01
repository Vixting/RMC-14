using System.Linq;
using Content.Client._RMC14.Xenonids.UI;
using Content.Client.Message;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Strain;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Evolution;

[UsedImplicitly]
public sealed class XenoEvolutionBui : BoundUserInterface
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;

    [ViewVariables]
    private XenoEvolutionWindow? _window;

    private readonly Dictionary<EntProtoId, XenoChoiceControl> _evolutionControls = new();
    private readonly Dictionary<EntProtoId, XenoChoiceControl> _strainControls = new();
    private readonly Dictionary<EntProtoId, XenoChoiceControl> _earlyEvolveControls = new();
    private readonly Dictionary<EntProtoId, XenoChoiceControl> _raffleControls = new();

    public XenoEvolutionBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<XenoEvolutionWindow>();
        _window.OvipositorNeededLabel.Visible = false;

        if (EntMan.TryGetComponent(Owner, out XenoEvolutionComponent? xeno))
        {
            foreach (var strain in xeno.Strains)
            {
                AddStrain(strain);
            }
        }

        _window.StrainsLabel.Visible = _window.StrainsContainer.ChildCount > 0;
        Refresh();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        Refresh();
    }

    private void AddEvolution(EntProtoId evolutionId)
    {
        if (!_prototype.TryIndex(evolutionId, out var evolution))
            return;

        if (!_evolutionControls.TryGetValue(evolutionId, out var control))
        {
            control = new XenoChoiceControl();
            control.Set(evolution.Name, _sprite.Frame0(evolution));
            control.Button.Disabled = false;

            control.Button.OnPressed += _ =>
            {
                SendPredictedMessage(new XenoEvolveBuiMsg(evolutionId));
                Close();
            };

            _evolutionControls[evolutionId] = control;
            _window?.EvolutionsContainer.AddChild(control);
        }

        control.Visible = true;
        control.Button.Disabled = false;
    }

    private void AddEarlyEvolve(EntProtoId evolutionId)
    {
        if (_window is not { IsOpen: true })
            return;

        if (!_prototype.TryIndex(evolutionId, out var evolution))
            return;

        if (!_earlyEvolveControls.TryGetValue(evolutionId, out var control))
        {
            control = new XenoChoiceControl();
            control.Set(evolution.Name, _sprite.Frame0(evolution));
            control.Button.Disabled = false;

            control.Button.OnPressed += _ =>
            {
                SendPredictedMessage(new XenoEvolveBuiMsg(evolutionId));
                Close();
            };

            _earlyEvolveControls[evolutionId] = control;
            _window.EarlyEvolvesContainer.AddChild(control);
        }

        control.Visible = true;
        control.Button.Disabled = false;
    }

    private void AddRaffleEntry(EntProtoId evolutionId, EntProtoId? currentChoice)
    {
        if (_window is not { IsOpen: true })
            return;

        if (!_prototype.TryIndex(evolutionId, out var evolution))
            return;

        if (!_raffleControls.TryGetValue(evolutionId, out var control))
        {
            control = new XenoChoiceControl();
            control.Button.Disabled = false;

            control.Button.OnPressed += _ =>
            {
                SendPredictedMessage(new XenoT3RaffleEntryMsg(evolutionId));
                Close();
            };

            _raffleControls[evolutionId] = control;
            _window.RaffleContainer.AddChild(control);
        }

        var isSelected = currentChoice == evolutionId;
        var label = isSelected
            ? $"[bold]{evolution.Name}[/bold] ✓"
            : evolution.Name;
        control.Set(label, _sprite.Frame0(evolution));
        control.Visible = true;
        control.Button.Disabled = false;
    }

    private void AddStrain(EntProtoId strainId)
    {
        if (_window is not { IsOpen: true })
            return;

        if (!_prototype.TryIndex(strainId, out var strain))
            return;

        if (!_strainControls.TryGetValue(strainId, out var control))
        {
            control = new XenoChoiceControl();

            var name = strain.Name;
            string? description = null;

            if (strain.TryGetComponent(out XenoStrainComponent? strainComp))
            {
                name = $"{Loc.GetString(strainComp.Name)} {name}";
                description = strainComp.Description;
            }

            control.Set(name, _sprite.Frame0(strain));
            control.Button.Disabled = false;

            control.Button.OnPressed += _ =>
            {
                var confirmWindow = new XenoStrainConfirmWindow();
                confirmWindow.SetInfo(name, _sprite.Frame0(strain), description);

                confirmWindow.OnConfirm += () =>
                {
                    SendPredictedMessage(new XenoStrainBuiMsg(strainId));
                    confirmWindow.Close();
                    Close();
                };

                confirmWindow.OpenCentered();
            };

            _strainControls[strainId] = control;
            _window.StrainsContainer.AddChild(control);
        }

        control.Visible = true;
        control.Button.Disabled = false;
    }

    public void Refresh()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out XenoEvolutionComponent? xeno))
            return;

        var buiState = State as XenoEvolveBuiState;
        var weedKillerDeployed = buiState?.WeedKillerDeployed ?? true;
        var hasHiveQueen = buiState?.HasHiveQueen ?? false;
        var t3Unlocked = buiState?.T3Unlocked ?? false;
        var currentRaffleChoice = xeno.T3RaffleChoice;

        _window.PointsLabel.Visible = xeno.Max > FixedPoint2.Zero;

        foreach (var control in _evolutionControls.Values)
            control.Visible = false;

        foreach (var control in _earlyEvolveControls.Values)
            control.Visible = false;

        foreach (var control in _raffleControls.Values)
            control.Visible = false;

        foreach (var evolutionId in xeno.EvolvesToWithoutPoints)
        {
            if (IsQueenProto(evolutionId) && hasHiveQueen)
                continue;

            AddEvolution(evolutionId);
        }

        if (xeno.Points >= xeno.Max)
        {
            foreach (var evolutionId in xeno.EvolvesTo)
            {
                if (!t3Unlocked && IsT3Proto(evolutionId))
                    continue;

                AddEvolution(evolutionId);
            }
        }

        // T3 raffle section — only shown before T3 unlocks and if this xeno has T3 targets
        var hasT3Options = xeno.EvolvesTo.Any(IsT3Proto);
        _window.RaffleSection.Visible = !t3Unlocked && hasT3Options;
        if (!t3Unlocked && hasT3Options)
        {
            foreach (var evolutionId in xeno.EvolvesTo)
            {
                if (!IsT3Proto(evolutionId))
                    continue;

                AddRaffleEntry(evolutionId, currentRaffleChoice);
            }

            // Show registered status
            if (currentRaffleChoice != null &&
                _prototype.TryIndex(currentRaffleChoice.Value, out var chosen))
            {
                _window.RaffleStatusLabel.SetMarkupPermissive(
                    Loc.GetString("rmc-xeno-t3-raffle-status", ("choice", chosen.Name)));
                _window.RaffleStatusLabel.Visible = true;
            }
            else
            {
                _window.RaffleStatusLabel.Visible = false;
            }
        }
        else
        {
            _window.RaffleStatusLabel.Visible = false;
        }

        _window.EarlyEvolveSection.Visible = !weedKillerDeployed && xeno.EarlyEvolvesTo.Count > 0;
        if (!weedKillerDeployed && xeno.Points >= xeno.Max)
        {
            foreach (var evolutionId in xeno.EarlyEvolvesTo)
            {
                AddEarlyEvolve(evolutionId);
            }
        }

        _window.Separator.Visible = _window.EvolutionsContainer.Children.Any(child => child.Visible) &&
                                    _window.StrainsContainer.Children.Any(child => child.Visible);

        var lackingOvipositor = buiState?.LackingOvipositor ?? false;
        var points = xeno.Points;

        _window.PointsLabel.Text = Loc.GetString("rmc-xeno-ui-evolution-points",
            ("points", (int)Math.Floor(points.Double())),
            ("maxPoints", xeno.Max));

        if (lackingOvipositor && xeno.Max > FixedPoint2.Zero)
        {
            if (!_window.OvipositorNeededLabel.Visible)
            {
                _window.OvipositorNeededLabel.SetMarkupPermissive(Loc.GetString("rmc-xeno-ui-ovi-needed-label"));
                _window.OvipositorNeededLabel.Visible = true;
            }
        }
        else if (_window.OvipositorNeededLabel.Visible)
        {
            _window.OvipositorNeededLabel.Visible = false;
        }
    }

    private bool IsT3Proto(EntProtoId protoId)
    {
        if (!_prototype.TryIndex(protoId, out var proto))
            return false;

        return proto.TryGetComponent<XenoComponent>(out var xenoComp, _compFactory) && xenoComp.Tier >= 3;
    }

    private bool IsQueenProto(EntProtoId protoId)
    {
        if (!_prototype.TryIndex(protoId, out var proto))
            return false;

        return proto.TryGetComponent<XenoEvolutionGranterComponent>(out _, _compFactory);
    }
}
