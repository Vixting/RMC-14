using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Content.MapRenderer.Painters;
using Content.Server.Maps;
using Content.Shared._RMC14.Rules;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.MapRenderer;

internal static class RenderedMapExporter
{
    private const string TacMapOutputDirectoryName = "tacmap";

    private sealed class TacMapTarget
    {
        public required string Id;
        public required string Name;
        public required string ResourcePath;
        public required string FilePath;
        public readonly HashSet<string> MatchKeys = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class DiscoveredMap
    {
        public required string ResourcePath;
        public string Name = string.Empty;
        public int NamePriority;
        public readonly HashSet<string> MatchKeys = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class CachedInsertRender
    {
        public required string PatchFile;
        public required MapBounds RenderBounds;
        public required List<MapSpawnCell> Spawns;
    }

    public static string GetOutputRoot(CommandLineArguments arguments)
    {
        return Path.Combine(arguments.OutputPath, TacMapOutputDirectoryName);
    }

    public static string GetStandaloneViewerPath()
    {
        return Path.GetFullPath(Path.Combine("Tools", "TacticalMapViewer", "index.html"));
    }

    public static async Task Run(CommandLineArguments arguments, ExternalTestContext testContext)
    {
        var targets = await DiscoverTargets(arguments, testContext);
        if (targets.Count == 0)
        {
            Console.WriteLine("No maps matched the provided input.");
            return;
        }

        var outputRoot = GetOutputRoot(arguments);
        var mapsDirectory = Path.Combine(outputRoot, "maps");
        var imagesDirectory = Path.Combine(outputRoot, "images");
        var iconsDirectory = Path.Combine(outputRoot, "icons");
        var insertSourceDirectory = Path.Combine(outputRoot, "insert_sources");
        var insertOverlayDirectory = Path.Combine(outputRoot, "insert_overlays");
        Directory.CreateDirectory(mapsDirectory);
        Directory.CreateDirectory(imagesDirectory);
        Directory.CreateDirectory(iconsDirectory);
        Directory.CreateDirectory(insertSourceDirectory);
        Directory.CreateDirectory(insertOverlayDirectory);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var manifest = new MapExportManifest
        {
            GeneratedUtc = DateTime.UtcNow
        };
        var savedSpawnIcons = new Dictionary<string, string>(StringComparer.Ordinal);
        var usedIconNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cachedInsertRenders = new Dictionary<string, CachedInsertRender>(StringComparer.OrdinalIgnoreCase);
        var usedInsertPatchNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedInsertOverlayNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Console.WriteLine($"Exporting rendered maps to {outputRoot}");

        foreach (var target in targets)
        {
            Console.WriteLine($"[map-export] Loading {target.Name} ({target.ResourcePath})");

            var renderTarget = new RenderMapFile { FileName = target.FilePath };
            await using var painter = new MapPainter(renderTarget, testContext);
            Dictionary<string, Image<Rgba32>>? spawnIcons = null;

            try
            {
                await painter.Initialize();
                var mapData = await painter.ExportRenderedMapData(target.Id, target.Name);
                spawnIcons = painter.TakeSpawnIcons();

                if (mapData.Grids.Count == 0)
                {
                    DisposeSpawnIcons(spawnIcons);
                    spawnIcons = null;
                    Console.WriteLine($"[map-export] Skipping {target.Id}: no grids were exported.");
                    continue;
                }

                await SaveSpawnIcons(spawnIcons, iconsDirectory, savedSpawnIcons, usedIconNames);
                spawnIcons = null;
                ApplySpawnIconPaths(mapData, savedSpawnIcons);

                var gridsById = mapData.Grids.ToDictionary(g => g.GridId, StringComparer.Ordinal);
                await foreach (var renderedGrid in painter.Paint())
                {
                    if (renderedGrid.GridUid is not { } gridUid ||
                        !painter.TryGetGridExportId(gridUid, out var gridId) ||
                        !gridsById.TryGetValue(gridId, out var gridExport))
                    {
                        renderedGrid.Image.Dispose();
                        continue;
                    }

                    var imageFileName = $"{target.Id}-{gridId}.png";
                    var imageFilePath = Path.Combine(imagesDirectory, imageFileName);
                    await renderedGrid.Image.SaveAsPngAsync(imageFilePath);

                    gridExport.Image = new MapRenderImage
                    {
                        File = Path.Combine("images", imageFileName).Replace('\\', '/'),
                        Width = renderedGrid.Image.Width,
                        Height = renderedGrid.Image.Height,
                        PixelsPerTile = TilePainter.TileImageSize
                    };

                    renderedGrid.Image.Dispose();
                }

                await ExportInsertOverlays(
                    mapData,
                    target,
                    testContext,
                    outputRoot,
                    insertSourceDirectory,
                    insertOverlayDirectory,
                    savedSpawnIcons,
                    usedIconNames,
                    cachedInsertRenders,
                    usedInsertPatchNames,
                    usedInsertOverlayNames);

                var mapFileName = $"{target.Id}.json";
                var mapFilePath = Path.Combine(mapsDirectory, mapFileName);
                var mapJson = JsonSerializer.Serialize(mapData, jsonOptions);
                await File.WriteAllTextAsync(mapFilePath, mapJson);

                manifest.Maps.Add(new MapExportManifestMap
                {
                    Id = target.Id,
                    Name = target.Name,
                    File = Path.Combine("maps", mapFileName).Replace('\\', '/')
                });

                Console.WriteLine($"[map-export] Wrote {mapData.Grids.Count} grids to {mapFilePath}");
            }
            catch (Exception ex)
            {
                if (spawnIcons != null)
                    DisposeSpawnIcons(spawnIcons);
                Console.WriteLine($"[map-export] Failed to export {target.ResourcePath}:");
                Console.WriteLine(ex);
            }
            finally
            {
                try
                {
                    await painter.CleanReturnAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[map-export] Cleanup error for {target.Id}: {ex}");
                }
            }
        }

        manifest.Maps.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        manifest = RebuildManifestFromExports(outputRoot, jsonOptions, manifest.GeneratedUtc);

        var manifestPath = Path.Combine(outputRoot, "manifest.json");
        var manifestJson = JsonSerializer.Serialize(manifest, jsonOptions);
        await File.WriteAllTextAsync(manifestPath, manifestJson);

        Console.WriteLine($"[map-export] Export complete. Maps exported: {manifest.Maps.Count}");
        Console.WriteLine($"[map-export] Open the standalone viewer: {GetStandaloneViewerPath()}");
        Console.WriteLine($"[map-export] Then load export folder: {outputRoot}");
    }

    private static async Task ExportInsertOverlays(
        MapExport mapData,
        TacMapTarget target,
        ExternalTestContext testContext,
        string outputRoot,
        string insertSourceDirectory,
        string insertOverlayDirectory,
        Dictionary<string, string> savedSpawnIcons,
        HashSet<string> usedIconNames,
        Dictionary<string, CachedInsertRender> cachedInsertRenders,
        HashSet<string> usedInsertPatchNames,
        HashSet<string> usedInsertOverlayNames)
    {
        foreach (var grid in mapData.Grids)
        {
            if (grid.Image == null)
                continue;

            foreach (var cell in grid.Inserts)
            {
                foreach (var insert in cell.Inserts)
                {
                    for (var i = 0; i < insert.Variations.Count; i++)
                    {
                        var variation = insert.Variations[i];
                        if (!TryNormalizeResourcePath(variation.Spawn, out var resourcePath))
                            continue;

                        var diskPath = ResourcePathToDiskPath(resourcePath);
                        if (!File.Exists(diskPath))
                            continue;

                        var cached = await GetOrCreateInsertRender(
                            resourcePath,
                            diskPath,
                            testContext,
                            insertSourceDirectory,
                            savedSpawnIcons,
                            usedIconNames,
                            cachedInsertRenders,
                            usedInsertPatchNames);

                        if (cached == null)
                            continue;

                        var overlayFileName = $"{MakeUniqueId(SanitizeId($"{target.Id}_{grid.GridId}_{cell.X}_{cell.Y}_{insert.PrototypeId ?? insert.Name}_{i}"), usedInsertOverlayNames)}.png";
                        var overlayDiskPath = Path.Combine(insertOverlayDirectory, overlayFileName);
                        using var overlay = new Image<Rgba32>(grid.Image.Width, grid.Image.Height, new Rgba32(0, 0, 0, 0));
                        using (var patch = await Image.LoadAsync<Rgba32>(cached.PatchFile))
                        {
                            var drawX = (int) Math.Round((cell.X + variation.OffsetX + cached.RenderBounds.MinX - grid.RenderBounds.MinX) * grid.Image.PixelsPerTile);
                            var drawY = (int) Math.Round((grid.RenderBounds.MaxY - (cell.Y + variation.OffsetY + cached.RenderBounds.MaxY)) * grid.Image.PixelsPerTile);
                            overlay.Mutate(ctx => ctx.DrawImage(patch, new Point(drawX, drawY), 1f));
                        }

                        await overlay.SaveAsPngAsync(overlayDiskPath);
                        variation.Overlay = Path.Combine("insert_overlays", overlayFileName).Replace('\\', '/');
                        variation.Spawns = TransformInsertSpawnCells(cached.Spawns, cell.X, cell.Y, variation.OffsetX, variation.OffsetY, grid.RenderBounds);
                    }
                }
            }
        }
    }

    private static async Task<CachedInsertRender?> GetOrCreateInsertRender(
        string resourcePath,
        string diskPath,
        ExternalTestContext testContext,
        string insertSourceDirectory,
        Dictionary<string, string> savedSpawnIcons,
        HashSet<string> usedIconNames,
        Dictionary<string, CachedInsertRender> cachedInsertRenders,
        HashSet<string> usedInsertPatchNames)
    {
        if (cachedInsertRenders.TryGetValue(diskPath, out var cached))
            return cached;

        var renderTarget = new RenderMapFile { FileName = diskPath };
        await using var painter = new MapPainter(renderTarget, testContext);

        Dictionary<string, Image<Rgba32>>? spawnIcons = null;
        try
        {
            await painter.Initialize();
            var insertMap = await painter.ExportRenderedMapData(Path.GetFileNameWithoutExtension(diskPath), Path.GetFileNameWithoutExtension(diskPath));
            spawnIcons = painter.TakeSpawnIcons();

            await SaveSpawnIcons(spawnIcons, Path.Combine(Path.GetDirectoryName(insertSourceDirectory)!, "icons"), savedSpawnIcons, usedIconNames);
            spawnIcons = null;
            ApplySpawnIconPaths(insertMap, savedSpawnIcons);

            var grid = insertMap.Grids.FirstOrDefault();
            if (grid == null)
                return null;

            await foreach (var renderedGrid in painter.Paint())
            {
                var patchName = $"{MakeUniqueId(SanitizeId(Path.GetFileNameWithoutExtension(diskPath)), usedInsertPatchNames)}.png";
                var patchPath = Path.Combine(insertSourceDirectory, patchName);
                await renderedGrid.Image.SaveAsPngAsync(patchPath);
                renderedGrid.Image.Dispose();

                cached = new CachedInsertRender
                {
                    PatchFile = patchPath,
                    RenderBounds = grid.RenderBounds,
                    Spawns = CloneSpawnCells(grid.Spawns),
                };
                cachedInsertRenders[diskPath] = cached;
                return cached;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[map-export] Failed to render insert overlay source {resourcePath}: {ex}");
        }
        finally
        {
            if (spawnIcons != null)
                DisposeSpawnIcons(spawnIcons);

            await painter.CleanReturnAsync();
        }

        return null;
    }

    private static List<MapSpawnCell> CloneSpawnCells(IEnumerable<MapSpawnCell> cells)
    {
        return cells.Select(cell => new MapSpawnCell(
            cell.X,
            cell.Y,
            cell.Spawns.Select(CloneSpawnInfo).ToList())).ToList();
    }

    private static List<MapSpawnCell> TransformInsertSpawnCells(
        IEnumerable<MapSpawnCell> cells,
        int insertTileX,
        int insertTileY,
        float offsetX,
        float offsetY,
        MapBounds targetBounds)
    {
        var transformed = new List<MapSpawnCell>();
        foreach (var cell in cells)
        {
            var x = (int) MathF.Floor(insertTileX + offsetX + cell.X);
            var y = (int) MathF.Floor(insertTileY + offsetY + cell.Y);
            if (x < targetBounds.MinX || x > targetBounds.MaxX || y < targetBounds.MinY || y > targetBounds.MaxY)
                continue;

            transformed.Add(new MapSpawnCell(
                x,
                y,
                cell.Spawns.Select(spawn =>
                {
                    var clone = CloneSpawnInfo(spawn);
                    clone.Origin = "sourceInsert";
                    return clone;
                }).ToList()));
        }

        transformed.Sort(static (a, b) =>
        {
            var compareX = a.X.CompareTo(b.X);
            return compareX != 0 ? compareX : a.Y.CompareTo(b.Y);
        });
        return transformed;
    }

    private static MapSpawnInfo CloneSpawnInfo(MapSpawnInfo spawn)
    {
        return new MapSpawnInfo
        {
            Name = spawn.Name,
            PrototypeId = spawn.PrototypeId,
            Kind = spawn.Kind,
            Origin = spawn.Origin,
            SpawnType = spawn.SpawnType,
            JobId = spawn.JobId,
            IntelType = spawn.IntelType,
            Chance = spawn.Chance,
            RareChance = spawn.RareChance,
            MinCount = spawn.MinCount,
            MaxCount = spawn.MaxCount,
            Quota = spawn.Quota,
            Ratio = spawn.Ratio,
            DeleteAfterSpawn = spawn.DeleteAfterSpawn,
            TargetId = spawn.TargetId,
            GroupId = spawn.GroupId,
            SpawnPath = spawn.SpawnPath,
            Targets = new List<string>(spawn.Targets),
            RareTargets = new List<string>(spawn.RareTargets),
            Icon = spawn.Icon,
        };
    }

    private static async Task SaveSpawnIcons(
        Dictionary<string, Image<Rgba32>> spawnIcons,
        string iconsDirectory,
        Dictionary<string, string> savedSpawnIcons,
        HashSet<string> usedIconNames)
    {
        foreach (var (prototypeId, icon) in spawnIcons)
        {
            try
            {
                if (savedSpawnIcons.ContainsKey(prototypeId))
                    continue;

                var fileName = $"{MakeUniqueId(SanitizeId(prototypeId), usedIconNames)}.png";
                var iconPath = Path.Combine(iconsDirectory, fileName);
                await icon.SaveAsPngAsync(iconPath);
                savedSpawnIcons[prototypeId] = Path.Combine("icons", fileName).Replace('\\', '/');
            }
            finally
            {
                icon.Dispose();
            }
        }
    }

    private static void ApplySpawnIconPaths(MapExport mapData, Dictionary<string, string> savedSpawnIcons)
    {
        foreach (var grid in mapData.Grids)
        {
            foreach (var cell in grid.Spawns)
            {
                foreach (var spawn in cell.Spawns)
                {
                    if (spawn.PrototypeId != null &&
                        savedSpawnIcons.TryGetValue(spawn.PrototypeId, out var iconPath))
                    {
                        spawn.Icon = iconPath;
                    }
                }
            }
        }
    }

    private static void DisposeSpawnIcons(Dictionary<string, Image<Rgba32>> spawnIcons)
    {
        foreach (var icon in spawnIcons.Values)
        {
            icon.Dispose();
        }
    }

    private static MapExportManifest RebuildManifestFromExports(
        string outputRoot,
        JsonSerializerOptions jsonOptions,
        DateTime generatedUtc)
    {
        var manifest = new MapExportManifest
        {
            GeneratedUtc = generatedUtc
        };

        var mapsDirectory = Path.Combine(outputRoot, "maps");
        if (!Directory.Exists(mapsDirectory))
            return manifest;

        foreach (var file in Directory.GetFiles(mapsDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var map = JsonSerializer.Deserialize<MapExport>(json, jsonOptions);
                if (map == null || string.IsNullOrWhiteSpace(map.Id))
                    continue;

                manifest.Maps.Add(new MapExportManifestMap
                {
                    Id = map.Id,
                    Name = string.IsNullOrWhiteSpace(map.Name) ? map.Id : map.Name,
                    File = Path.Combine("maps", Path.GetFileName(file)).Replace('\\', '/')
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[map-export] Failed to include exported map in manifest: {file}");
                Console.WriteLine(ex);
            }
        }

        manifest.Maps.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return manifest;
    }

    private static async Task<List<TacMapTarget>> DiscoverTargets(CommandLineArguments arguments, ExternalTestContext testContext)
    {
        var discovered = await DiscoverPrototypeMaps(testContext);
        var selected = SelectMaps(arguments, discovered);

        selected.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return selected;
    }

    private static async Task<List<DiscoveredMap>> DiscoverPrototypeMaps(ExternalTestContext testContext)
    {
        var discovered = new Dictionary<string, DiscoveredMap>(StringComparer.OrdinalIgnoreCase);

        await using var pair = await PoolManager.GetServerClient(testContext: testContext);
        var prototypes = pair.Server.ResolveDependency<IPrototypeManager>();
        var componentFactory = pair.Server.ResolveDependency<IComponentFactory>();

        await pair.Server.WaitPost(() =>
        {
            foreach (var gameMap in prototypes.EnumeratePrototypes<GameMapPrototype>())
            {
                var path = gameMap.MapPath.ToString();
                if (!IsRmcMapPath(path))
                    continue;

                AddOrUpdate(discovered, path, gameMap.MapName, 1, gameMap.ID);
            }

            foreach (var entity in prototypes.EnumeratePrototypes<EntityPrototype>())
            {
                if (!entity.TryGetComponent(out RMCPlanetMapPrototypeComponent? planet, componentFactory))
                    continue;

                var path = planet.Map.ToString();
                if (!IsRmcMapPath(path))
                    continue;

                AddOrUpdate(discovered, path, entity.Name, 2, entity.ID);
            }
        });

        return discovered.Values.ToList();
    }

    private static void AddOrUpdate(
        Dictionary<string, DiscoveredMap> discovered,
        string resourcePath,
        string? name,
        int namePriority,
        string additionalKey)
    {
        if (!discovered.TryGetValue(resourcePath, out var entry))
        {
            entry = new DiscoveredMap
            {
                ResourcePath = resourcePath,
                Name = !string.IsNullOrWhiteSpace(name) ? name : Path.GetFileNameWithoutExtension(resourcePath),
                NamePriority = namePriority
            };
            discovered[resourcePath] = entry;
        }
        else if (namePriority > entry.NamePriority && !string.IsNullOrWhiteSpace(name))
        {
            entry.Name = name;
            entry.NamePriority = namePriority;
        }

        var fileName = Path.GetFileNameWithoutExtension(resourcePath);
        if (!string.IsNullOrWhiteSpace(fileName))
            entry.MatchKeys.Add(fileName);

        entry.MatchKeys.Add(resourcePath);
        entry.MatchKeys.Add(additionalKey);
        if (!string.IsNullOrWhiteSpace(name))
            entry.MatchKeys.Add(name);
    }

    private static List<TacMapTarget> SelectMaps(CommandLineArguments arguments, List<DiscoveredMap> discovered)
    {
        var targets = new List<TacMapTarget>();
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var selectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var byPath = discovered.ToDictionary(x => x.ResourcePath, StringComparer.OrdinalIgnoreCase);

        if (arguments.Maps.Count == 0)
        {
            foreach (var map in discovered)
            {
                if (!TryCreateTarget(map.ResourcePath, map.Name, map.MatchKeys, usedIds, out var target))
                    continue;

                selectedPaths.Add(map.ResourcePath);
                targets.Add(target);
            }

            return targets;
        }

        foreach (var token in arguments.Maps)
        {
            var matchedAny = false;

            foreach (var candidate in discovered)
            {
                if (!candidate.MatchKeys.Contains(token))
                    continue;

                matchedAny = true;
                if (selectedPaths.Contains(candidate.ResourcePath))
                    continue;

                if (!TryCreateTarget(candidate.ResourcePath, candidate.Name, candidate.MatchKeys, usedIds, out var target))
                    continue;

                selectedPaths.Add(candidate.ResourcePath);
                targets.Add(target);
            }

            if (matchedAny)
                continue;

            if (TryNormalizeResourcePath(token, out var resourcePath))
            {
                if (byPath.TryGetValue(resourcePath, out var known))
                {
                    if (!selectedPaths.Contains(known.ResourcePath) &&
                        TryCreateTarget(known.ResourcePath, known.Name, known.MatchKeys, usedIds, out var target))
                    {
                        selectedPaths.Add(known.ResourcePath);
                        targets.Add(target);
                    }
                    continue;
                }

                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { token, resourcePath };
                var fallbackName = Path.GetFileNameWithoutExtension(resourcePath);
                if (!selectedPaths.Contains(resourcePath) &&
                    TryCreateTarget(resourcePath, fallbackName, keys, usedIds, out var directTarget))
                {
                    selectedPaths.Add(resourcePath);
                    targets.Add(directTarget);
                }

                continue;
            }

            Console.WriteLine($"[map-export] Ignoring unknown map token '{token}'.");
        }

        return targets;
    }

    private static bool TryCreateTarget(
        string resourcePath,
        string? displayName,
        IEnumerable<string> keys,
        HashSet<string> usedIds,
        out TacMapTarget target)
    {
        target = default!;
        var diskPath = ResourcePathToDiskPath(resourcePath);
        if (!File.Exists(diskPath))
        {
            Console.WriteLine($"[map-export] Skipping missing map file: {resourcePath} -> {diskPath}");
            return false;
        }

        var baseId = Path.GetFileNameWithoutExtension(resourcePath);
        var id = MakeUniqueId(SanitizeId(baseId), usedIds);
        var name = string.IsNullOrWhiteSpace(displayName) ? baseId : displayName.Trim();

        target = new TacMapTarget
        {
            Id = id,
            Name = name,
            ResourcePath = resourcePath,
            FilePath = diskPath
        };

        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key))
                target.MatchKeys.Add(key);
        }

        target.MatchKeys.Add(resourcePath);
        target.MatchKeys.Add(id);
        target.MatchKeys.Add(baseId);
        return true;
    }

    private static bool TryNormalizeResourcePath(string token, out string resourcePath)
    {
        resourcePath = string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (token.StartsWith("/Maps/", StringComparison.OrdinalIgnoreCase))
        {
            resourcePath = token.Replace('\\', '/');
            return true;
        }

        if (File.Exists(token))
        {
            var full = Path.GetFullPath(token);
            var resourcesRoot = Path.GetFullPath("Resources");
            if (!full.StartsWith(resourcesRoot, StringComparison.OrdinalIgnoreCase))
                return false;

            var relative = Path.GetRelativePath(resourcesRoot, full).Replace('\\', '/');
            resourcePath = "/" + relative;
            return true;
        }

        return false;
    }

    private static bool IsRmcMapPath(string path)
    {
        return path.StartsWith("/Maps/_RMC14/", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResourcePathToDiskPath(string resourcePath)
    {
        var normalized = resourcePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine("Resources", normalized));
    }

    private static string MakeUniqueId(string id, HashSet<string> used)
    {
        if (used.Add(id))
            return id;

        var i = 2;
        while (true)
        {
            var candidate = $"{id}_{i}";
            if (used.Add(candidate))
                return candidate;
            i++;
        }
    }

    private static string SanitizeId(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "map";

        Span<char> tmp = stackalloc char[raw.Length];
        var len = 0;
        foreach (var ch in raw)
        {
            var lower = char.ToLowerInvariant(ch);
            if ((lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9'))
                tmp[len++] = lower;
            else
                tmp[len++] = '_';
        }

        var value = new string(tmp[..len]).Trim('_');
        if (value.Length == 0)
            return "map";
        return value;
    }
}
