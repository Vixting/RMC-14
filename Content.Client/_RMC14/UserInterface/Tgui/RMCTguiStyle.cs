using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client._RMC14.UserInterface.Tgui;

public static class RMCTguiStyle
{
    public const string RegularFontPath = "/Fonts/NotoSans/NotoSans-Regular.ttf";
    public const string BoldFontPath = "/Fonts/NotoSans/NotoSans-Bold.ttf";

    public const int FontSize = 10;
    public const int SmallFontSize = 9;
    public const int TitleFontSize = 11;
    public const int CloseFontSize = 16;

    public const float GainLighten = 0.35f;
    public const float TextLighten = 0.45f;
    public const float HeatAlpha = 0.4f;

    public static readonly Color Foreground = Color.White;
    public static readonly Color WindowBackground = Color.FromHex("#262626");
    public static readonly Color WindowBorder = Color.FromHex("#171717");
    public static readonly Color TitleBarBackground = Color.FromHex("#363636");
    public static readonly Color TitleText = Color.White.WithAlpha(0.75f);
    public static readonly Color CloseText = Color.White.WithAlpha(0.375f);
    public static readonly Color CloseHoverBackground = Color.FromHex("#CC0000");
    public static readonly Color SectionBackground = Color.Black.WithAlpha(0.33f);
    public static readonly Color Primary = Color.FromHex("#4972A2");
    public static readonly Color LossFill = Color.FromHex("#808080").WithAlpha(0.45f);
    public static readonly Color TextShadow = Color.Black.WithAlpha(0.75f);
    public static readonly Color DimText = Color.White.WithAlpha(0.35f);
    public static readonly Color BaseValueText = Color.White.WithAlpha(0.7f);
    public static readonly Color GroupTitle = Color.White.WithAlpha(0.55f);

    public static readonly Color Good = Color.FromHex("#4E9121");
    public static readonly Color Average = Color.FromHex("#CC790D");
    public static readonly Color Bad = Color.FromHex("#BD2020");
    public static readonly Color Neutral = Color.FromHex("#3E618A");
    public static readonly Color NeutralHover = Color.FromHex("#92AECE");
    public static readonly Color MenuBackground = Color.FromHex("#0A0A0A");
    public static readonly Color MenuItemHover = Color.White.WithAlpha(0.2f);
    public static readonly Color MenuItemSelected = Color.White.WithAlpha(0.5f);

    public static Font RegularFont(IResourceCache cache, int size = FontSize)
    {
        return cache.GetFont(RegularFontPath, size);
    }

    public static Font BoldFont(IResourceCache cache, int size = FontSize)
    {
        return cache.GetFont(BoldFontPath, size);
    }

    public static Color Light(Color color)
    {
        return Color.InterpolateBetween(color, Color.White, TextLighten);
    }

    public static Color HeatColor(float fraction)
    {
        fraction = Math.Clamp(fraction, 0, 1);
        return fraction >= 0.5f
            ? Color.InterpolateBetween(Average, Good, (fraction - 0.5f) * 2)
            : Color.InterpolateBetween(Bad, Average, fraction * 2);
    }

    public static StyleBoxFlat Box(Color background, float horizontal = 0, float vertical = 0)
    {
        return new StyleBoxFlat(background)
        {
            ContentMarginLeftOverride = horizontal,
            ContentMarginRightOverride = horizontal,
            ContentMarginTopOverride = vertical,
            ContentMarginBottomOverride = vertical,
        };
    }
}
