using Content.Shared.Maps;
using Robust.Client.Utility;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._RMC14.Particles;

/// <summary>
/// Samples the avrg color of tile textures so particles can match the ground they spawn on
/// </summary>
public sealed class ParticleTileColorSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IResourceManager _resource = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;

    private readonly Dictionary<int, Color?> _tileColors = new();

    public Tile? GetTile(MapCoordinates coords)
    {
        if (!_mapManager.TryFindGridAt(coords, out var gridUid, out var grid))
            return null;

        var tile = _map.GetTileRef(gridUid, grid, coords).Tile;
        return tile.IsEmpty ? null : tile;
    }

    public ContentTileDefinition? GetDefinition(Tile tile)
    {
        return _tileDefinition[tile.TypeId] as ContentTileDefinition;
    }

    public Color? GetTileColor(Tile tile)
    {
        if (_tileColors.TryGetValue(tile.TypeId, out var cached))
            return cached;

        Color? color = null;
        if (GetDefinition(tile) is { Sprite: { } sprite } &&
            _resource.TryContentFileRead(sprite, out var stream))
        {
            using (stream)
            using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream))
            {
                double r = 0, g = 0, b = 0, weight = 0;
                foreach (var pixel in image.GetPixelSpan())
                {
                    var a = pixel.A / 255.0;
                    r += pixel.R * a;
                    g += pixel.G * a;
                    b += pixel.B * a;
                    weight += a;
                }

                if (weight > 0)
                    color = new Color((float) (r / weight / 255), (float) (g / weight / 255), (float) (b / weight / 255));
            }
        }

        _tileColors[tile.TypeId] = color;
        return color;
    }
}
