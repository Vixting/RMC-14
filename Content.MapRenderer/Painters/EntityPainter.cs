using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static Robust.UnitTesting.RobustIntegrationTest;

namespace Content.MapRenderer.Painters;

public sealed class EntityPainter
{
    private readonly IResourceManager _resManager;

    private readonly Dictionary<(string path, string state), Image<Rgba32>> _images;
    private readonly Image<Rgba32> _errorImage;

    private readonly IEntityManager _sEntityManager;
    private readonly SpriteSystem _sprite;

    public EntityPainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
    {
        _resManager = client.ResolveDependency<IResourceManager>();

        _sEntityManager = server.ResolveDependency<IEntityManager>();
        _sprite = client.ResolveDependency<IEntityManager>().System<SpriteSystem>();

        _images = new Dictionary<(string path, string state), Image<Rgba32>>();
        _errorImage = Image.Load<Rgba32>(_resManager.ContentFileRead("/Textures/error.rsi/error.png"));
    }

    public void Run(Image canvas, List<EntityData> entities, Vector2 customOffset = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // TODO cache this shit what are we insane
        entities.Sort(Comparer<EntityData>.Create((x, y) => x.Sprite.DrawDepth.CompareTo(y.Sprite.DrawDepth)));
        var xformSystem = _sEntityManager.System<SharedTransformSystem>();

        foreach (var entity in entities)
        {
            Run(canvas, entity, xformSystem, customOffset);
        }

        Console.WriteLine($"{nameof(EntityPainter)} painted {entities.Count} entities in {(int)stopwatch.Elapsed.TotalMilliseconds} ms");
    }

    public void Run(Image canvas, EntityData entity, SharedTransformSystem xformSystem, Vector2 customOffset = default)
    {
        if (!entity.Sprite.Visible || entity.Sprite.ContainerOccluded)
        {
            return;
        }

        var worldRotation = xformSystem.GetWorldRotation(entity.Owner);
        foreach (var layer in entity.Sprite.AllLayers)
        {
            if (!layer.Visible)
            {
                continue;
            }

            if (!layer.RsiState.IsValid)
            {
                continue;
            }

            var image = GetLayerImage(layer);

            var dir = _sprite.LayerGetDirectionCount((SpriteComponent.Layer)layer) switch
            {
                0 => 0,
                _ => (int)layer.EffectiveDirection(worldRotation)
            };

            var rsi = layer.ActualRsi;
            var (x, y, width, height) = GetRsiFrame(rsi, image, layer, dir);

            var rect = new Rectangle(x, y, width, height);
            if (!new Rectangle(Point.Empty, image.Size).Contains(rect))
            {
                Console.WriteLine($"Invalid layer {rsi!.Path}/{layer.RsiState.Name}.png for entity {_sEntityManager.ToPrettyString(entity.Owner)} at ({entity.X}, {entity.Y})");
                return;
            }

            image.Mutate(o => o.Crop(rect));

            var spriteRotation = 0f;
            if (!entity.Sprite.NoRotation && !entity.Sprite.SnapCardinals && _sprite.LayerGetDirectionCount((SpriteComponent.Layer)layer) == 1)
            {
                spriteRotation = (float)worldRotation.Degrees;
            }

            var colorMix = entity.Sprite.Color * layer.Color;
            var imageColor = Color.FromRgba(colorMix.RByte, colorMix.GByte, colorMix.BByte, colorMix.AByte);
            var coloredImage = new Image<Rgba32>(image.Width, image.Height);
            coloredImage.Mutate(o => o.BackgroundColor(imageColor));

            var (imgX, imgY) = rsi?.Size ?? (EyeManager.PixelsPerMeter, EyeManager.PixelsPerMeter);
            var offsetX = (int)(entity.Sprite.Offset.X + customOffset.X) * EyeManager.PixelsPerMeter;
            var offsetY = (int)(entity.Sprite.Offset.Y + customOffset.Y) * EyeManager.PixelsPerMeter;
            image.Mutate(o => o
                .DrawImage(coloredImage, PixelColorBlendingMode.Multiply, PixelAlphaCompositionMode.SrcAtop, 1)
                .Resize(imgX, imgY)
                .Flip(FlipMode.Vertical)
                .Rotate(spriteRotation));

            var pointX = (int)entity.X + offsetX - imgX / 2;
            var pointY = (int)entity.Y + offsetY - imgY / 2;
            canvas.Mutate(o => o.DrawImage(image, new Point(pointX, pointY), 1));
        }
    }

    public Image<Rgba32>? RenderIcon(EntityData entity, int canvasSize = 64, int padding = 2)
    {
        if (!entity.Sprite.Visible || entity.Sprite.ContainerOccluded)
            return null;

        var canvas = new Image<Rgba32>(canvasSize, canvasSize);
        var xformSystem = _sEntityManager.System<SharedTransformSystem>();
        var worldRotation = xformSystem.GetWorldRotation(entity.Owner);
        var centerX = canvas.Width / 2;
        var centerY = canvas.Height / 2;

        foreach (var layer in entity.Sprite.AllLayers)
        {
            if (!layer.Visible || !layer.RsiState.IsValid)
                continue;

            var image = GetLayerImage(layer);
            var rsi = layer.ActualRsi;
            var dir = _sprite.LayerGetDirectionCount((SpriteComponent.Layer)layer) switch
            {
                0 => 0,
                _ => (int)layer.EffectiveDirection(worldRotation)
            };

            var (x, y, width, height) = GetRsiFrame(rsi, image, layer, dir);
            var rect = new Rectangle(x, y, width, height);
            if (!new Rectangle(Point.Empty, image.Size).Contains(rect))
                continue;

            image.Mutate(o => o.Crop(rect));

            var spriteRotation = 0f;
            if (!entity.Sprite.NoRotation &&
                !entity.Sprite.SnapCardinals &&
                _sprite.LayerGetDirectionCount((SpriteComponent.Layer)layer) == 1)
            {
                spriteRotation = (float)worldRotation.Degrees;
            }

            var colorMix = entity.Sprite.Color * layer.Color;
            var imageColor = Color.FromRgba(colorMix.RByte, colorMix.GByte, colorMix.BByte, colorMix.AByte);
            var coloredImage = new Image<Rgba32>(image.Width, image.Height);
            coloredImage.Mutate(o => o.BackgroundColor(imageColor));

            var (imgX, imgY) = rsi?.Size ?? (EyeManager.PixelsPerMeter, EyeManager.PixelsPerMeter);
            var offsetX = (int)entity.Sprite.Offset.X * EyeManager.PixelsPerMeter;
            var offsetY = (int)entity.Sprite.Offset.Y * EyeManager.PixelsPerMeter;
            image.Mutate(o => o
                .DrawImage(coloredImage, PixelColorBlendingMode.Multiply, PixelAlphaCompositionMode.SrcAtop, 1)
                .Resize(imgX, imgY)
                .Rotate(spriteRotation));

            var pointX = centerX + offsetX - imgX / 2;
            var pointY = centerY + offsetY - imgY / 2;
            canvas.Mutate(o => o.DrawImage(image, new Point(pointX, pointY), 1));
            coloredImage.Dispose();
            image.Dispose();
        }

        var cropped = CropTransparentBounds(canvas, padding);
        canvas.Dispose();
        return cropped;
    }

    private Image<Rgba32> GetLayerImage(ISpriteLayer layer)
    {
        var rsi = layer.ActualRsi;
        if (rsi == null || !rsi.TryGetState(layer.RsiState, out var state))
            return _errorImage.Clone();

        var key = (rsi.Path!.ToString(), state.StateId.Name!);
        if (!_images.TryGetValue(key, out var image))
        {
            var stream = _resManager.ContentFileRead($"{rsi.Path}/{state.StateId}.png");
            image = Image.Load<Rgba32>(stream);
            _images[key] = image;
        }

        return image.Clone();
    }

    private (int X, int Y, int Width, int Height) GetRsiFrame(RSI? rsi, Image<Rgba32> image, ISpriteLayer layer, int direction)
    {
        if (rsi is null)
            return (0, 0, EyeManager.PixelsPerMeter, EyeManager.PixelsPerMeter);

        var statesX = image.Width / rsi.Size.X;
        var statesY = image.Height / rsi.Size.Y;
        var stateCount = statesX * statesY;
        var frames = stateCount / _sprite.LayerGetDirectionCount((SpriteComponent.Layer)layer);
        var target = direction * frames;
        var targetY = target / statesX;
        var targetX = target % statesX;
        return (targetX * rsi.Size.X, targetY * rsi.Size.Y, rsi.Size.X, rsi.Size.Y);
    }

    private static Image<Rgba32> CropTransparentBounds(Image<Rgba32> image, int padding)
    {
        var minX = image.Width;
        var minY = image.Height;
        var maxX = -1;
        var maxY = -1;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (row[x].A == 0)
                        continue;

                    if (x < minX)
                        minX = x;
                    if (x > maxX)
                        maxX = x;
                    if (y < minY)
                        minY = y;
                    if (y > maxY)
                        maxY = y;
                }
            }
        });

        if (maxX < minX || maxY < minY)
            return image.Clone();

        minX = Math.Max(0, minX - padding);
        minY = Math.Max(0, minY - padding);
        maxX = Math.Min(image.Width - 1, maxX + padding);
        maxY = Math.Min(image.Height - 1, maxY + padding);

        return image.Clone(o => o.Crop(new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1)));
    }
}
