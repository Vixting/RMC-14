using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Xenonids.Biomass;

public sealed class RMCBiomassCategoryOptionButton : OptionButton
{
    private static readonly Color Amber = Color.FromHex("#FAC000");
    private static readonly Color AmberDim = Color.FromHex("#8A6800");
    private static readonly Color AmberBlack = Color.FromHex("#221801");

    public override void ButtonOverride(Button button)
    {
        base.ButtonOverride(button);

        button.Modulate = Color.White;
        button.ModulateSelfOverride = Color.White;
        button.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = AmberBlack,
            BorderColor = AmberDim,
            BorderThickness = new Thickness(1),
        };
        button.Label.FontColorOverride = Amber;
    }
}
