using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Xenonids.Biomass;

public sealed class RMCBiomassCategoryOptionButton : OptionButton
{
    private static readonly Color Amber = Color.FromHex("#FFD21A");
    private static readonly Color AmberDim = Color.FromHex("#8A6800");
    private static readonly Color AmberBlack = Color.FromHex("#150E00");

    public RMCBiomassCategoryOptionButton()
    {
        RecolorChildren(this);
    }

    private static void RecolorChildren(Control control)
    {
        foreach (var child in control.Children)
        {
            switch (child)
            {
                case Label label:
                    label.FontColorOverride = AmberBlack;
                    break;
                case TextureRect triangle:
                    triangle.Modulate = AmberBlack;
                    break;
                default:
                    RecolorChildren(child);
                    break;
            }
        }
    }

    public override void ButtonOverride(Button button)
    {
        base.ButtonOverride(button);

        button.Modulate = Color.White;
        button.ModulateSelfOverride = Color.White;
        button.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = AmberBlack,
            BorderColor = AmberDim,
            BorderThickness = new Thickness(2),
        };
        button.Label.FontColorOverride = Amber;
    }
}
