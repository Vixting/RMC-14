using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Botany;

internal static class BotanyUiStyle
{
    public static readonly Color HeaderColor = Color.FromHex("#12141A");
    public static readonly Color GreenColor = Color.FromHex("#4CAF50");
    public static readonly Color TanColor = Color.FromHex("#ffb950");
    public static readonly Color RedColor = Color.FromHex("#8B2020");

    private static readonly Color DisabledModulate = new(1f, 1f, 1f, 0.45f);

    public static StyleBoxFlat Flat(Color color)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = color,
            ContentMarginLeftOverride = 6,
            ContentMarginRightOverride = 6,
            ContentMarginTopOverride = 1,
            ContentMarginBottomOverride = 1,
        };
    }

    public static void Header(PanelContainer panel)
    {
        panel.PanelOverride = Flat(HeaderColor);
    }

    public static void StatusRow(PanelContainer panel, Label label, string text, Color color)
    {
        label.Text = text;
        label.FontColorOverride = color == RedColor ? Color.White : Color.Black;
        panel.PanelOverride = Flat(color);
    }

    public static void SetButtonEnabled(Button button, bool enabled)
    {
        button.Disabled = !enabled;
        button.Modulate = enabled ? Color.White : DisabledModulate;
    }

    public static IconHandle IconButton(IResourceCache resourceCache, Button button, string texturePath, string whiteTexturePath, string text)
    {
        button.Text = null;
        button.StyleBoxOverride = Flat(TanColor);
        button.ModulateSelfOverride = Color.White;

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
            FontColorOverride = Color.Black,
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

    public sealed class IconHandle
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
}
