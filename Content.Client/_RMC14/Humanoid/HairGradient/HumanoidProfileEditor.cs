using System.Linq;
using Content.Shared._RMC14.Humanoid.HairGradient;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

// ReSharper disable CheckNamespace
namespace Content.Client.Lobby.UI;
// ReSharper restore CheckNamespace

public sealed partial class HumanoidProfileEditor
{
    private readonly List<ProtoId<RMCHairGradientPrototype>?> _hairGradients = new();
    private ColorSelectorSliders _hairGradientColorSelector = default!;

    private void InitializeHairGradient()
    {
        HairGradientColorContainer.AddChild(_hairGradientColorSelector = new ColorSelectorSliders());
        _hairGradientColorSelector.SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv;
        _hairGradientColorSelector.OnColorChanged += color =>
        {
            if (Profile is null)
                return;

            Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithHairGradientColor(color));
            ReloadProfilePreview();
        };

        HairGradientButton.OnItemSelected += args =>
        {
            if (Profile is null)
                return;

            HairGradientButton.SelectId(args.Id);
            Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithHairGradient(_hairGradients[args.Id]));
            ReloadProfilePreview();
        };
    }

    private void UpdateHairGradientControls()
    {
        HairGradientButton.Clear();
        _hairGradients.Clear();

        _hairGradients.Add(null);
        HairGradientButton.AddItem(Loc.GetString("rmc-humanoid-profile-editor-hair-gradient-none"), 0);

        var gradients = _prototypeManager.EnumeratePrototypes<RMCHairGradientPrototype>()
            .OrderBy(gradient => Loc.GetString(gradient.Name));

        foreach (var gradient in gradients)
        {
            HairGradientButton.AddItem(Loc.GetString(gradient.Name), _hairGradients.Count);
            _hairGradients.Add(gradient.ID);
        }

        if (Profile == null)
        {
            HairGradientContainer.Visible = false;
            return;
        }

        HairGradientContainer.Visible = RMCHairGradientSystem.CanUseHairGradient(Profile);
        HairGradientButton.SelectId(Math.Max(0, _hairGradients.IndexOf(Profile.Appearance.HairGradient)));
        _hairGradientColorSelector.Color = Profile.Appearance.HairGradientColor;
    }
}
