using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.UserInterface.Tgui;

public sealed class RMCTguiStatFill : Control
{
    public float Ratio;
    public float? BaseRatio;
    public Color FillColor;

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var current = Math.Clamp(Ratio, 0, 1) * PixelWidth;
        var baseline = BaseRatio is { } baseRatio ? Math.Clamp(baseRatio, 0, 1) * PixelWidth : current;

        handle.DrawRect(new UIBox2(0, 0, Math.Min(current, baseline), PixelHeight), FillColor);

        if (current > baseline)
        {
            var gain = Color.InterpolateBetween(FillColor, Color.White, RMCTguiStyle.GainLighten);
            handle.DrawRect(new UIBox2(baseline, 0, current, PixelHeight), gain);
        }
        else if (current < baseline)
        {
            handle.DrawRect(new UIBox2(current, 0, baseline, PixelHeight), RMCTguiStyle.LossFill);
        }
    }
}
