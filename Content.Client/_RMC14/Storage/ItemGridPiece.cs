using System.Numerics;
using Content.Shared.Storage;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

// ReSharper disable CheckNamespace

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed partial class ItemGridPiece
{
    private readonly Entity<StorageComponent>? _ghostStorage;
    public bool GhostHardReserved;

    private bool TryGetPieceStorage(out Entity<StorageComponent> storage)
    {
        if (_ghostStorage is { } ghost)
        {
            storage = ghost;
            return true;
        }

        if (_entityManager.System<ContainerSystem>().TryGetContainingContainer((Entity, null), out var container) &&
            _entityManager.TryGetComponent(container.Owner, out StorageComponent? storageComp))
        {
            storage = (container.Owner, storageComp);
            return true;
        }

        storage = default;
        return false;
    }

    private Color? GetGhostModulate()
    {
        if (_ghostStorage == null)
            return null;

        var alpha = UserInterfaceManager.CurrentlyHovered == this ? 0.65f : 0.35f;
        return GhostHardReserved
            ? new Color(1f, 0.25f, 0.25f, alpha)
            : new Color(1f, 1f, 1f, alpha);
    }

    private Color? TintGhostSprite()
    {
        if (GetGhostModulate() is not { } tint ||
            !_entityManager.TryGetComponent(Entity, out SpriteComponent? sprite))
        {
            return null;
        }

        var original = sprite.Color;
        _entityManager.System<SpriteSystem>().SetColor((Entity, sprite),
            new Color(original.R * tint.R, original.G * tint.G, original.B * tint.B, tint.A));

        return original;
    }

    private void RestoreGhostSprite(Color? original)
    {
        if (original is not { } color ||
            !_entityManager.TryGetComponent(Entity, out SpriteComponent? sprite))
        {
            return;
        }

        _entityManager.System<SpriteSystem>().SetColor((Entity, sprite), color);
    }

    private void DrawGhostOutline(DrawingHandleScreen handle, Vector2 size, Box2i boundingGrid)
    {
        if (UserInterfaceManager.CurrentlyHovered != this)
            return;

        var outlineSize = size * 2 * new Vector2(boundingGrid.Width + 1, boundingGrid.Height + 1);
        var outlineColor = GhostHardReserved ? new Color(1f, 0.35f, 0.35f) : Color.White;
        handle.DrawRect(new UIBox2(PixelPosition, PixelPosition + outlineSize), outlineColor.WithAlpha(0.9f), filled: false);
    }
}
