using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Client.Resources;
using Content.Client.Storage.Systems;
using Content.Shared._RMC14.Storage;
using Content.Shared.Input;
using Content.Shared.Item;
using Content.Shared.Storage;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;

// ReSharper disable CheckNamespace

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed partial class StorageWindow
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private const string SaveLayoutTexturePath = "/Textures/Interface/NavMap/beveled_star.png";

    private (Box2i Grid, EntityUid[] Contained, Dictionary<EntityUid, ItemStorageLocation> Stored) _lastUpdate =
        (default, Array.Empty<EntityUid>(), new Dictionary<EntityUid, ItemStorageLocation>());

    // faded placeholders shown at slots reserved for items currently held/taken out
    private readonly Dictionary<EntityUid, ItemGridPiece> _ghostPieces = new();
    private readonly List<EntityUid> _ghostsToRemove = new();

    private PanelContainer? WrapBorders(Control control, int? fixedSizeX, int x, int right)
    {
        if (fixedSizeX != null &&
            x != 0 &&
            (x != right || x % fixedSizeX == 0))
        {
            var thickness = x % fixedSizeX == 0
                ? new Thickness(1, 0, 0, 0)
                : new Thickness(0, 0, 1, 0);
            var panel = new PanelContainer();
            panel.PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Black,
                BorderThickness = thickness
            };
            panel.AddChild(control);
            return panel;
        }

        return null;
    }

    private bool IsNestedStorage()
    {
        if (!_entity.System<StorageSystem>().NestedStorage || StorageEntity is not { } storage)
            return false;

        return _entity.System<SharedContainerSystem>().TryGetContainingContainer(storage, out var parentContainer) &&
               _entity.TryGetComponent(parentContainer.Owner, out StorageComponent? parentStorage) &&
               parentStorage.Container.Contains(storage);
    }

    private Control CreateSaveLayoutContainer(int rows)
    {
        var saveLayoutButton = new TextureButton
        {
            Name = "SaveLayoutButton",
            TextureNormal = _resourceCache.GetTexture(SaveLayoutTexturePath),
            Scale = new Vector2(0.8f, 0.8f),
            Modulate = Color.FromHex("#a8a8a8"),
            ToolTip = Loc.GetString("rmc-ui-storage-save-layout"),
        };
        saveLayoutButton.OnPressed += _ =>
        {
            if (StorageEntity is { } storageEntity)
                _entity.RaisePredictiveEvent(new RMCStorageSaveLayoutEvent(_entity.GetNetEntity(storageEntity)));
        };
        saveLayoutButton.OnKeyBindDown += args =>
        {
            if (args.Function != ContentKeyFunctions.RotateStoredItem)
                return;

            if (StorageEntity is { } storageEntity)
            {
                _entity.RaisePredictiveEvent(new RMCStorageSaveLayoutEvent(_entity.GetNetEntity(storageEntity), hard: true));
                args.Handle();
            }
        };

        return new BoxContainer
        {
            Name = "SaveLayoutContainer",
            Children =
            {
                new TextureRect
                {
                    Texture = rows > 2 ? _sidebarMiddleTexture : _sidebarBottomTexture,
                    TextureScale = new Vector2(2, 2),
                    Children =
                    {
                        saveLayoutButton,
                    }
                }
            }
        };
    }

    private void UpdateReservedGhosts(StorageComponent storageComp, Box2i boundingGrid, Vector2 size)
    {
        var reserved = _entity.GetComponentOrNull<RMCStorageReservedSlotsComponent>(StorageEntity);
        var storageSystem = _entity.System<StorageSystem>();

        _ghostsToRemove.Clear();

        foreach (var (item, control) in _ghostPieces)
        {
            if (reserved != null &&
                StorageEntity is { } se &&
                !storageComp.StoredItems.ContainsKey(item) &&
                reserved.Reserved.TryGetValue(item, out var stillLoc) &&
                _entity.EntityExists(item) &&
                _entity.TryGetComponent<ItemComponent>(item, out var stillItemComp) &&
                storageSystem.ItemFitsInGridLocation((item, stillItemComp), (se, storageComp), stillLoc))
            {
                control.GhostHardReserved = reserved.HardReserved.Contains(item);

                if (control.Location.Equals(stillLoc))
                    continue;

                var index = stillLoc.Position.X + stillLoc.Position.Y * (boundingGrid.Width + 1);
                if (index >= 0 && index < _controlGrid.Count)
                {
                    control.Location = stillLoc;
                    control.Orphan();
                    _controlGrid[index].AddChild(control);
                    continue;
                }
            }

            _ghostsToRemove.Add(item);
        }

        foreach (var item in _ghostsToRemove)
        {
            _ghostPieces.Remove(item, out var control);
            control?.Orphan();
        }

        if (reserved == null || StorageEntity is not { } storageEntity)
            return;

        foreach (var (item, loc) in reserved.Reserved)
        {
            if (_ghostPieces.ContainsKey(item))
                continue;

            if (storageComp.StoredItems.ContainsKey(item))
                continue;

            if (!_entity.EntityExists(item) || !_entity.TryGetComponent<ItemComponent>(item, out var itemComp))
                continue;

            if (!storageSystem.ItemFitsInGridLocation((item, itemComp), (storageEntity, storageComp), loc))
                continue;

            var controlIndex = loc.Position.X + loc.Position.Y * (boundingGrid.Width + 1);
            if (controlIndex < 0 || controlIndex >= _controlGrid.Count)
                continue;

            var ghostPiece = new ItemGridPiece((item, itemComp), loc, _entity, (storageEntity, storageComp))
            {
                MinSize = size,
                GhostHardReserved = reserved.HardReserved.Contains(item),
            };

            var ghostItem = item;
            var ghostItemName = _entity.GetComponent<MetaDataComponent>(item).EntityName;
            ghostPiece.OnPiecePressed += (args, _) =>
            {
                if (args.Function == ContentKeyFunctions.RotateStoredItem)
                {
                    args.Handle();
                    _entity.RaisePredictiveEvent(new RMCStorageToggleHardReservedEvent(
                        _entity.GetNetEntity(storageEntity),
                        _entity.GetNetEntity(ghostItem)));
                    return;
                }

                if (args.Function != ContentKeyFunctions.MoveStoredItem)
                    return;

                var handEntity = _entity.System<HandsSystem>().GetActiveHandEntity();
                var handMatches = handEntity == ghostItem ||
                    (handEntity is { } held &&
                     _entity.TryGetComponent<MetaDataComponent>(held, out var heldMeta) &&
                     heldMeta.EntityName == ghostItemName);

                if (handMatches)
                {
                    return;
                }

                args.Handle();

                _entity.RaisePredictiveEvent(new RMCStorageClearReservedSlotEvent(
                    _entity.GetNetEntity(storageEntity),
                    _entity.GetNetEntity(ghostItem)));
            };

            _controlGrid[controlIndex].AddChild(ghostPiece);
            _ghostPieces[item] = ghostPiece;
        }
    }
}
