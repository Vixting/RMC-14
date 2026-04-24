using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Content.Client.Markers;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.MapRenderer;
using Content.Server._RMC14.Fog;
using Content.Server._RMC14.Figurines;
using Content.Server._RMC14.Humanoid;
using Content.Server._RMC14.MapInsert;
using Content.Server._RMC14.Spawners;
using Content.Server.Cloning.Components;
using Content.Server.Spawners.Components;
using Content.Server.GameTicking;
using Content.Server.Humanoid.Components;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Intel;
using Content.Shared._RMC14.Spawners;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Robust.Client.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Delivery;
using Content.Shared._RMC14.Communications;
using Content.Shared._RMC14.Marines.Squads;

namespace Content.MapRenderer.Painters
{
    public sealed class MapPainter : IAsyncDisposable
    {
        private static readonly FieldInfo? AreaExcludeFromTacMapRenderField =
            typeof(AreaComponent).GetField("ExcludeFromTacMapRender", BindingFlags.Instance | BindingFlags.Public);

        private static readonly FieldInfo? AreaMinimapColorField =
            typeof(AreaComponent).GetField(nameof(AreaComponent.MinimapColor), BindingFlags.Instance | BindingFlags.Public);

        private static readonly FieldInfo? AreaGridHasTacMapBoundsField =
            typeof(AreaGridComponent).GetField("HasTacMapBounds", BindingFlags.Instance | BindingFlags.Public);

        private static readonly FieldInfo? AreaGridTacMapBoundsMinField =
            typeof(AreaGridComponent).GetField("TacMapBoundsMin", BindingFlags.Instance | BindingFlags.Public);

        private static readonly FieldInfo? AreaGridTacMapBoundsMaxField =
            typeof(AreaGridComponent).GetField("TacMapBoundsMax", BindingFlags.Instance | BindingFlags.Public);

        private readonly RenderMap _map;
        private readonly ITestContextLike _testContextLike;

        private TestPair? _pair;
        private Entity<MapGridComponent>[] _grids = [];
        private readonly Dictionary<EntityUid, string> _gridIds = new();
        private readonly Dictionary<string, EntityUid> _spawnIconEntities = new(StringComparer.Ordinal);
        private readonly HashSet<string> _spawnIconPrototypeIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Image<Rgba32>> _spawnIcons = new(StringComparer.Ordinal);
        private string? _sourceFilePath;

        private sealed class SourceEntityData
        {
            public required int YamlUid;
            public string? PrototypeId;
            public int? ParentYamlUid;
            public Vector2 LocalPosition;
            public bool IsGrid;
        }

        private sealed class SourceMapData
        {
            public Dictionary<int, SourceEntityData> Entities { get; } = new();
        }

        public MapPainter(RenderMap map, ITestContextLike testContextLike)
        {
            _map = map;
            _testContextLike = testContextLike;
        }

        public async Task Initialize()
        {
            var stopwatch = RStopwatch.StartNew();

            var poolSettings = new PoolSettings
            {
                DummyTicker = false,
                Connected = true,
                Destructive = true,
                Fresh = true,
                // Seriously whoever made MapPainter use GameMapPrototype I wish you step on a lego one time.
                Map = _map is RenderMapPrototype prototype ? prototype.Prototype : PoolManager.TestMap,
            };
            _pair = await PoolManager.GetServerClient(poolSettings, _testContextLike);

            Console.WriteLine($"Loaded client and server in {(int)stopwatch.Elapsed.TotalMilliseconds} ms");

            if (_map is RenderMapFile mapFile)
            {
                _sourceFilePath = mapFile.FileName;
                using var stream = File.OpenRead(mapFile.FileName);

                await _pair.Server.WaitPost(() =>
                {
                    var loadOptions = new MapLoadOptions
                    {
                        // Accept loading both maps and grids without caring about what the input file truly is.
                        DeserializationOptions =
                        {
                            LogOrphanedGrids = false,
                            StoreYamlUids = true,
                        },
                    };

                    if (!_pair.Server.System<MapLoaderSystem>().TryLoadGeneric(stream, mapFile.FileName, out var loadResult, loadOptions))
                        throw new IOException($"File {mapFile.FileName} could not be read");

                    _grids = loadResult.Grids.ToArray();
                });
            }
        }

        public async Task SetupView(bool showMarkers)
        {
            if (_pair == null)
                throw new InvalidOperationException("Instance not initialized!");

            await _pair.Client.WaitPost(() =>
            {
                if (_pair.Client.EntMan.TryGetComponent(_pair.Client.PlayerMan.LocalEntity, out SpriteComponent? sprite))
                {
                    _pair.Client.System<SpriteSystem>()
                        .SetVisible((_pair.Client.PlayerMan.LocalEntity.Value, sprite), false);
                }
            });

            if (showMarkers)
            {
                await _pair.Client.WaitPost(() =>
                {
                    _pair.Client.System<MarkerSystem>().MarkersVisible = true;
                });
            }
        }

        public async Task<MapViewerData> GenerateMapViewerData(ParallaxOutput? parallaxOutput)
        {
            if (_pair == null)
                throw new InvalidOperationException("Instance not initialized!");

            var mapShort = _map.ShortName;

            string fullName;
            if (_map is RenderMapPrototype prototype)
            {
                fullName = _pair.Server.ProtoMan.Index(prototype.Prototype).MapName;
            }
            else
            {
                fullName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mapShort);
            }

            var mapViewerData = new MapViewerData
            {
                Id = mapShort,
                Name = fullName,
            };

            if (parallaxOutput != null)
            {
                await _pair.Client.WaitPost(() =>
                {
                    var res = _pair.Client.InstanceDependencyCollection.Resolve<IResourceManager>();
                    mapViewerData.ParallaxLayers.Add(LayerGroup.DefaultParallax(res, parallaxOutput));
                });
            }

            return mapViewerData;
        }

        public async Task<MapExport> ExportRenderedMapData(string mapId, string mapName)
        {
            if (_pair == null)
                throw new InvalidOperationException("Instance not initialized!");

            var export = new MapExport
            {
                Id = mapId,
                Name = mapName,
            };

            await _pair.RunTicksSync(10);
            await Task.WhenAll(_pair.Client.WaitIdleAsync(), _pair.Server.WaitIdleAsync());

            var serverEntity = _pair.Server.ResolveDependency<IServerEntityManager>();
            var entityManager = _pair.Server.ResolveDependency<IEntityManager>();
            var prototypes = _pair.Server.ResolveDependency<IPrototypeManager>();
            var compFactory = _pair.Server.ResolveDependency<IComponentFactory>();
            var mapSystem = entityManager.System<SharedMapSystem>();

            await _pair.Server.WaitPost(() =>
            {
                var gridIndex = 0;
                _gridIds.Clear();
                _spawnIconEntities.Clear();
                _spawnIconPrototypeIds.Clear();

                foreach (var (uid, grid) in _grids)
                {
                    gridIndex++;
                    var gridId = $"grid_{gridIndex}";
                    var renderBounds = ResolveRenderBounds(uid, grid, mapSystem);
                    serverEntity.TryGetComponent(uid, out AreaGridComponent? areaGrid);
                    var gridExport = areaGrid != null
                        ? BuildGridExport(gridId, areaGrid, renderBounds, prototypes, compFactory)
                        : BuildMinimalGridExport(gridId, renderBounds);
                    gridExport.Labels = BuildLabelCells(uid, grid, areaGrid, gridExport.Bounds, renderBounds, mapSystem, entityManager);
                    gridExport.Entities = BuildEntityCells(uid, grid, renderBounds, mapSystem, serverEntity);
                    gridExport.Inserts = BuildInsertCells(uid, grid, renderBounds, mapSystem, entityManager, prototypes);
                    gridExport.Spawns = BuildSpawnCells(uid, grid, renderBounds, mapSystem, entityManager, prototypes);
                    gridExport.Tunnels = BuildTunnelCells(uid, grid, renderBounds, mapSystem, entityManager, prototypes);
                    gridExport.Roofing = BuildRoofingCells(uid, grid, renderBounds, mapSystem, entityManager, prototypes);
                    export.Grids.Add(gridExport);
                    _gridIds[uid] = gridId;
                }

                MergeSourceMapSpawns(export, entityManager, prototypes, compFactory);
            });

            await BuildSpawnIcons();

            return export;
        }

        public bool TryGetGridExportId(EntityUid uid, out string gridId)
        {
            return _gridIds.TryGetValue(uid, out gridId!);
        }

        public Dictionary<string, Image<Rgba32>> TakeSpawnIcons()
        {
            var icons = new Dictionary<string, Image<Rgba32>>(_spawnIcons, StringComparer.Ordinal);
            _spawnIcons.Clear();
            return icons;
        }

        public async IAsyncEnumerable<RenderedGridImage<Rgba32>> Paint()
        {
            if (_pair == null)
                throw new InvalidOperationException("Instance not initialized!");

            var client = _pair.Client;
            var server = _pair.Server;

            var sEntityManager = server.ResolveDependency<IServerEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapSys = entityManager.System<SharedMapSystem>();

            await _pair.RunTicksSync(10);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            var sMapManager = server.ResolveDependency<IMapManager>();

            var tilePainter = new TilePainter(client, server);
            var entityPainter = new GridPainter(client, server);
            var xformQuery = sEntityManager.GetEntityQuery<TransformComponent>();
            var xformSystem = sEntityManager.System<SharedTransformSystem>();

            await server.WaitPost(() =>
            {
                var playerEntity = sPlayerManager.Sessions.Single().AttachedEntity;

                if (playerEntity.HasValue)
                {
                    sEntityManager.DeleteEntity(playerEntity.Value);
                }

                if (_map is RenderMapPrototype)
                {
                    var mapId = sEntityManager.System<GameTicker>().DefaultMap;
                    _grids = sMapManager.GetAllGrids(mapId).ToArray();
                }

                foreach (var (uid, _) in _grids)
                {
                    var gridXform = xformQuery.GetComponent(uid);
                    xformSystem.SetWorldRotation(gridXform, Angle.Zero);
                }
            });

            await _pair.RunTicksSync(10);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            foreach (var (uid, grid) in _grids)
            {
                var tiles = mapSys.GetAllTiles(uid, grid).ToList();
                if (tiles.Count == 0)
                {
                    Console.WriteLine($"Warning: Grid {uid} was empty. Skipping image rendering.");
                    continue;
                }
                var tileXSize = grid.TileSize * TilePainter.TileImageSize;
                var tileYSize = grid.TileSize * TilePainter.TileImageSize;

                var minX = tiles.Min(t => t.X);
                var minY = tiles.Min(t => t.Y);
                var maxX = tiles.Max(t => t.X);
                var maxY = tiles.Max(t => t.Y);
                var w = (maxX - minX + 1) * tileXSize;
                var h = (maxY - minY + 1) * tileYSize;
                var customOffset = new Vector2();

                //MapGrids don't have LocalAABB, so we offset them to align the bottom left corner with 0,0 coordinates
                if (grid.LocalAABB.IsEmpty())
                    customOffset = new Vector2(-minX, -minY);

                var gridCanvas = new Image<Rgba32>(w, h);

                await server.WaitPost(() =>
                {
                    tilePainter.Run(gridCanvas, uid, grid, customOffset);
                    entityPainter.Run(gridCanvas, uid, grid, customOffset);

                    gridCanvas.Mutate(e => e.Flip(FlipMode.Vertical));
                });

                var renderedImage = new RenderedGridImage<Rgba32>(gridCanvas)
                {
                    GridUid = uid,
                    Offset = xformSystem.GetWorldPosition(uid),
                };

                yield return renderedImage;
            }
        }

        public async Task CleanReturnAsync()
        {
            if (_pair == null)
                throw new InvalidOperationException("Instance not initialized!");

            await _pair.CleanReturnAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_pair != null)
                await _pair.DisposeAsync();
        }

        private static MapExportGrid BuildGridExport(
            string gridId,
            AreaGridComponent areaGrid,
            MapBounds renderBounds,
            IPrototypeManager prototypes,
            IComponentFactory componentFactory)
        {
            var bounds = ResolveBounds(areaGrid);
            var areaIds = new List<string>();
            var areaIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            var renderInfo = new Dictionary<string, (bool Exclude, uint Color)>(StringComparer.Ordinal);

            var areas = new List<MapAreaCell>(areaGrid.Areas.Count);
            foreach (var (pos, areaProto) in areaGrid.Areas)
            {
                if (!IsWithinBounds(pos, bounds))
                    continue;

                var id = areaProto.Id;
                if (!areaIndex.TryGetValue(id, out var idx))
                {
                    idx = areaIds.Count;
                    areaIds.Add(id);
                    areaIndex[id] = idx;
                }

                areas.Add(new MapAreaCell(pos.X, pos.Y, idx));
            }

            var colors = new List<MapColorCell>(areaGrid.Colors.Count);
            var colorPositions = new HashSet<Vector2i>();
            foreach (var (pos, color) in areaGrid.Colors)
            {
                if (!IsWithinBounds(pos, bounds))
                    continue;

                colors.Add(new MapColorCell(pos.X, pos.Y, PackColor(color)));
                colorPositions.Add(pos);
            }

            foreach (var (pos, areaProto) in areaGrid.Areas)
            {
                if (!IsWithinBounds(pos, bounds) || colorPositions.Contains(pos))
                    continue;

                var (exclude, color) = GetAreaRenderInfo(areaProto.Id, prototypes, componentFactory, renderInfo);
                if (exclude)
                    continue;

                colors.Add(new MapColorCell(pos.X, pos.Y, color));
            }

            var areaInfo = new List<MapAreaInfo>(areaIds.Count);
            foreach (var id in areaIds)
            {
                var info = new MapAreaInfo
                {
                    Id = id,
                    Name = id,
                };

                if (prototypes.TryIndex<EntityPrototype>(id, out var proto))
                {
                    info.Name = ResolveAreaName(proto, prototypes);

                    if (proto.TryGetComponent(out AreaComponent? areaComp, componentFactory))
                    {
                        info.Cas = areaComp.CAS;
                        info.MortarFire = areaComp.MortarFire;
                        info.MortarPlacement = areaComp.MortarPlacement;
                        info.Lasing = areaComp.Lasing;
                        info.Medevac = areaComp.Medevac;
                        info.Paradropping = areaComp.Paradropping;
                        info.OrbitalBombard = areaComp.OB;
                        info.SupplyDrop = areaComp.SupplyDrop;
                        info.Fulton = areaComp.Fulton;
                        info.AvoidBioscan = areaComp.AvoidBioscan;
                        info.NoTunnel = areaComp.NoTunnel;
                        info.Unweedable = areaComp.Unweedable;
                        info.BuildSpecial = areaComp.BuildSpecial;
                        info.ResinAllowed = areaComp.ResinAllowed;
                        info.ResinConstructionAllowed = areaComp.ResinConstructionAllowed;
                        info.WeatherEnabled = areaComp.WeatherEnabled;
                        info.HijackEvacuationArea = areaComp.HijackEvacuationArea;
                        info.AlwaysPowered = areaComp.AlwaysPowered;
                        info.HijackEvacuationWeight = areaComp.HijackEvacuationWeight;
                        info.HijackEvacuationType = FormatHijackEvacuationType(areaComp.HijackEvacuationType);
                        info.PowerNet = areaComp.PowerNet;
                        info.MinimapColor = PackColor(areaComp.MinimapColor);
                        info.ZLevel = areaComp.ZLevel;
                        info.LandingZone = areaComp.LandingZone;
                        info.LinkedLz = areaComp.LinkedLz;
                        info.WeedKilling = areaComp.WeedKilling;
                        info.RetrieveItemObjective = areaComp.RetrieveItemObjective;
                        info.BuildableTiles = areaComp.BuildableTiles;
                        info.ResinConstructCount = areaComp.ResinConstructCount;

                        if (AreaExcludeFromTacMapRenderField?.GetValue(areaComp) is bool excludeFromTacMapRender)
                            info.ExcludeFromTacMapRender = excludeFromTacMapRender;
                    }
                }

                areaInfo.Add(info);
            }

            return new MapExportGrid
            {
                GridId = gridId,
                HasMapBounds = HasTacMapBounds(areaGrid),
                Bounds = bounds,
                RenderBounds = renderBounds,
                Colors = colors,
                Areas = areas,
                AreaIds = areaIds,
                AreaInfo = areaInfo,
            };
        }

        private static MapExportGrid BuildMinimalGridExport(string gridId, MapBounds renderBounds)
        {
            return new MapExportGrid
            {
                GridId = gridId,
                HasMapBounds = false,
                Bounds = renderBounds,
                RenderBounds = renderBounds,
            };
        }

        private static MapBounds ResolveBounds(AreaGridComponent grid)
        {
            var hasAny = false;
            var fullMin = Vector2i.Zero;
            var fullMax = Vector2i.Zero;

            void Accumulate(Vector2i pos)
            {
                if (!hasAny)
                {
                    hasAny = true;
                    fullMin = pos;
                    fullMax = pos;
                }
                else
                {
                    fullMin = Vector2i.ComponentMin(fullMin, pos);
                    fullMax = Vector2i.ComponentMax(fullMax, pos);
                }
            }

            foreach (var (pos, _) in grid.Colors)
            {
                Accumulate(pos);
            }

            foreach (var (pos, _) in grid.Areas)
            {
                Accumulate(pos);
            }

            foreach (var (pos, _) in grid.Labels)
            {
                Accumulate(pos);
            }

            if (!hasAny)
                return new MapBounds(0, 0, 0, 0);

            if (TryGetTacMapBounds(grid, out var tacMapBoundsMin, out var tacMapBoundsMax))
            {
                var boundsMin = Vector2i.ComponentMax(fullMin, tacMapBoundsMin);
                var boundsMax = Vector2i.ComponentMin(fullMax, tacMapBoundsMax);

                if (boundsMax.X >= boundsMin.X && boundsMax.Y >= boundsMin.Y)
                {
                    return new MapBounds(
                        boundsMin.X,
                        boundsMin.Y,
                        boundsMax.X,
                        boundsMax.Y);
                }
            }

            return new MapBounds(fullMin.X, fullMin.Y, fullMax.X, fullMax.Y);
        }

        private static MapBounds ResolveRenderBounds(
            EntityUid gridUid,
            MapGridComponent grid,
            SharedMapSystem mapSystem)
        {
            var tiles = mapSystem.GetAllTiles(gridUid, grid).ToList();
            if (tiles.Count == 0)
                return new MapBounds(0, 0, 0, 0);

            var minX = tiles.Min(t => t.X);
            var minY = tiles.Min(t => t.Y);
            var maxX = tiles.Max(t => t.X);
            var maxY = tiles.Max(t => t.Y);
            return new MapBounds(minX, minY, maxX, maxY);
        }

        private static bool IsWithinBounds(Vector2i pos, MapBounds bounds)
        {
            return pos.X >= bounds.MinX &&
                   pos.X <= bounds.MaxX &&
                   pos.Y >= bounds.MinY &&
                   pos.Y <= bounds.MaxY;
        }

        private static List<MapEntityCell> BuildEntityCells(
            EntityUid gridUid,
            MapGridComponent grid,
            MapBounds renderBounds,
            SharedMapSystem mapSystem,
            IEntityManager entities)
        {
            var byTile = new Dictionary<Vector2i, List<MapEntityInfo>>();
            var query = entities.AllEntityQueryEnumerator<TransformComponent, MetaDataComponent>();

            while (query.MoveNext(out var uid, out var xform, out var meta))
            {
                if (uid == gridUid || xform.GridUid != gridUid)
                    continue;

                if (meta.EntityPrototype == null)
                    continue;

                var indices = mapSystem.CoordinatesToTile(gridUid, grid, xform.Coordinates);
                if (!IsWithinBounds(indices, renderBounds))
                    continue;

                if (!byTile.TryGetValue(indices, out var tileEntities))
                {
                    tileEntities = new List<MapEntityInfo>();
                    byTile[indices] = tileEntities;
                }

                tileEntities.Add(new MapEntityInfo
                {
                    Name = meta.EntityName,
                    PrototypeId = meta.EntityPrototype.ID
                });
            }

            var cells = new List<MapEntityCell>(byTile.Count);
            foreach (var (indices, tileEntities) in byTile)
            {
                tileEntities.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                cells.Add(new MapEntityCell(indices.X, indices.Y, tileEntities));
            }

            cells.Sort(static (a, b) =>
            {
                var compareX = a.X.CompareTo(b.X);
                return compareX != 0 ? compareX : a.Y.CompareTo(b.Y);
            });

            return cells;
        }

        private static List<MapLabelCell> BuildLabelCells(
            EntityUid gridUid,
            MapGridComponent grid,
            AreaGridComponent? areaGrid,
            MapBounds areaBounds,
            MapBounds renderBounds,
            SharedMapSystem mapSystem,
            IEntityManager entities)
        {
            var labelsByTile = new Dictionary<Vector2i, string>();

            if (areaGrid != null)
            {
                foreach (var (pos, text) in areaGrid.Labels)
                {
                    if (!IsWithinBounds(pos, areaBounds) || string.IsNullOrWhiteSpace(text))
                        continue;

                    labelsByTile[pos] = text;
                }
            }

            var query = entities.AllEntityQueryEnumerator<AreaLabelComponent, MetaDataComponent, TransformComponent>();
            while (query.MoveNext(out _, out _, out var meta, out var xform))
            {
                if (xform.GridUid != gridUid)
                    continue;

                if (string.IsNullOrWhiteSpace(meta.EntityName))
                    continue;

                var indices = mapSystem.CoordinatesToTile(gridUid, grid, xform.Coordinates);
                if (!IsWithinBounds(indices, renderBounds))
                    continue;

                labelsByTile[indices] = meta.EntityName;
            }

            var labels = new List<MapLabelCell>(labelsByTile.Count);
            foreach (var (pos, text) in labelsByTile)
            {
                labels.Add(new MapLabelCell(pos.X, pos.Y, text));
            }

            labels.Sort(static (a, b) =>
            {
                var compareX = a.X.CompareTo(b.X);
                return compareX != 0 ? compareX : a.Y.CompareTo(b.Y);
            });

            return labels;
        }

        private static List<MapInsertCell> BuildInsertCells(
            EntityUid gridUid,
            MapGridComponent grid,
            MapBounds renderBounds,
            SharedMapSystem mapSystem,
            IEntityManager entities,
            IPrototypeManager prototypes)
        {
            var byTile = new Dictionary<Vector2i, List<MapInsertInfo>>();
            var query = entities.AllEntityQueryEnumerator<MapInsertComponent, MetaDataComponent, TransformComponent>();

            while (query.MoveNext(out _, out var insert, out var meta, out var xform))
            {
                if (xform.GridUid != gridUid)
                    continue;

                var indices = mapSystem.CoordinatesToTile(gridUid, grid, xform.Coordinates);
                if (!IsWithinBounds(indices, renderBounds))
                    continue;

                if (!byTile.TryGetValue(indices, out var tileInserts))
                {
                    tileInserts = new List<MapInsertInfo>();
                    byTile[indices] = tileInserts;
                }

                var prototype = meta.EntityPrototype;
                tileInserts.Add(new MapInsertInfo
                {
                    Name = ResolveEntityDisplayName(meta, prototypes),
                    PrototypeId = prototype?.ID,
                    ClearEntities = insert.ClearEntities,
                    ClearDecals = insert.ClearDecals,
                    ReplaceAreas = insert.ReplaceAreas,
                    Variations = insert.Variations.Select(static variation => new MapInsertVariationInfo
                    {
                        Spawn = variation.Spawn.ToString(),
                        Probability = variation.Probability,
                        NightmareScenario = string.IsNullOrWhiteSpace(variation.NightmareScenario)
                            ? null
                            : variation.NightmareScenario,
                        OffsetX = variation.Offset.X,
                        OffsetY = variation.Offset.Y,
                    }).ToList(),
                });
            }

            var cells = new List<MapInsertCell>(byTile.Count);
            foreach (var (indices, tileInserts) in byTile)
            {
                tileInserts.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                cells.Add(new MapInsertCell(indices.X, indices.Y, tileInserts));
            }

            cells.Sort(static (a, b) =>
            {
                var compareX = a.X.CompareTo(b.X);
                return compareX != 0 ? compareX : a.Y.CompareTo(b.Y);
            });

            return cells;
        }

        private List<MapSpawnCell> BuildSpawnCells(
            EntityUid gridUid,
            MapGridComponent grid,
            MapBounds renderBounds,
            SharedMapSystem mapSystem,
            IEntityManager entities,
            IPrototypeManager prototypes)
        {
            var byTile = new Dictionary<Vector2i, List<MapSpawnInfo>>();
            var seen = new HashSet<EntityUid>();

            var jobSpawns = entities.AllEntityQueryEnumerator<SpawnPointComponent, MetaDataComponent, TransformComponent>();
            while (jobSpawns.MoveNext(out var uid, out var spawn, out var meta, out var xform))
            {
                AddSpawn(uid, meta, xform, spawn.SpawnType switch
                {
                    SpawnPointType.Job => "job",
                    SpawnPointType.LateJoin => "latejoin",
                    SpawnPointType.Observer => "observer",
                    _ => "spawn"
                }, FormatSpawnPointType(spawn.SpawnType), spawn.Job?.ToString(), null);
            }

            var xenoSpawns = entities.AllEntityQueryEnumerator<XenoSpawnPointComponent, MetaDataComponent, TransformComponent>();
            while (xenoSpawns.MoveNext(out var uid, out _, out var meta, out var xform))
            {
                AddSpawn(uid, meta, xform, "xeno", "Xeno", null, null);
            }

            var xenoLeaderSpawns = entities.AllEntityQueryEnumerator<XenoLeaderSpawnPointComponent, MetaDataComponent, TransformComponent>();
            while (xenoLeaderSpawns.MoveNext(out var uid, out _, out var meta, out var xform))
            {
                AddSpawn(uid, meta, xform, "xenoLeader", "XenoLeader", null, null);
            }

            var intelSpawns = entities.AllEntityQueryEnumerator<IntelSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (intelSpawns.MoveNext(out var uid, out var intel, out var meta, out var xform))
            {
                AddSpawn(uid, meta, xform, "intel", "Intel", null, FormatIntelSpawnerType(intel.IntelType));
            }

            var rmcJobSpawns = entities.AllEntityQueryEnumerator<RMCJobSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (rmcJobSpawns.MoveNext(out var uid, out var jobSpawn, out var meta, out var xform))
            {
                AddSpawn(uid, meta, xform, "job", "Job", jobSpawn.Job?.ToString(), null);
            }

            var gunSpawners = entities.AllEntityQueryEnumerator<GunSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (gunSpawners.MoveNext(out var uid, out var gunSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "gunSpawner",
                    "GunSpawner",
                    null,
                    null,
                    chance: gunSpawner.ChanceToSpawn,
                    minCount: gunSpawner.MinMagazines,
                    maxCount: gunSpawner.MaxMagazines,
                    deleteAfterSpawn: gunSpawner.DeleteAfterSpawn,
                    targets: ToSortedStringList(gunSpawner.Prototypes.Select(proto => proto.Gun.ToString())));
            }

            var randomSpawners = entities.AllEntityQueryEnumerator<RandomSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (randomSpawners.MoveNext(out var uid, out var randomSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "randomSpawner",
                    "RandomSpawner",
                    null,
                    null,
                    chance: randomSpawner.Chance,
                    rareChance: randomSpawner.RareChance,
                    deleteAfterSpawn: randomSpawner.DeleteSpawnerAfterSpawn,
                    targets: ToSortedStringList(randomSpawner.Prototypes.Select(proto => proto.ToString())),
                    rareTargets: ToSortedStringList(randomSpawner.RarePrototypes.Select(proto => proto.ToString())));
            }

            var uniqueRandomSpawners = entities.AllEntityQueryEnumerator<UniqueRandomSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (uniqueRandomSpawners.MoveNext(out var uid, out var uniqueRandomSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "uniqueRandomSpawner",
                    "UniqueRandomSpawner",
                    null,
                    null,
                    chance: uniqueRandomSpawner.Chance,
                    deleteAfterSpawn: uniqueRandomSpawner.DeleteSpawnerAfterSpawn,
                    groupId: uniqueRandomSpawner.SpawnerGroup.ToString(),
                    targets: ToSortedStringList(uniqueRandomSpawner.Prototypes.Select(proto => proto.ToString())));
            }

            var conditionalSpawners = entities.AllEntityQueryEnumerator<ConditionalSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (conditionalSpawners.MoveNext(out var uid, out var conditionalSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "conditionalSpawner",
                    "ConditionalSpawner",
                    null,
                    null,
                    chance: conditionalSpawner.Chance,
                    targets: ToSortedStringList(conditionalSpawner.Prototypes.Select(proto => proto.ToString())));
            }

            var entityTableSpawners = entities.AllEntityQueryEnumerator<EntityTableSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (entityTableSpawners.MoveNext(out var uid, out var entityTableSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "entityTableSpawner",
                    "EntityTableSpawner",
                    null,
                    null,
                    deleteAfterSpawn: entityTableSpawner.DeleteSpawnerAfterSpawn);
            }

            var itemPoolSpawners = entities.AllEntityQueryEnumerator<ItemPoolSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (itemPoolSpawners.MoveNext(out var uid, out var itemPoolSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "itemPoolSpawner",
                    "ItemPoolSpawner",
                    null,
                    null,
                    quota: itemPoolSpawner.Quota,
                    targets: ToSortedStringList(itemPoolSpawner.Prototypes.Select(proto => proto.ToString())));
            }

            var corpseSpawners = entities.AllEntityQueryEnumerator<CorpseSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (corpseSpawners.MoveNext(out var uid, out var corpseSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "corpseSpawner",
                    "CorpseSpawner",
                    null,
                    null,
                    targetId: corpseSpawner.Spawn?.ToString());
            }

            var aegisSpawners = entities.AllEntityQueryEnumerator<AegisSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (aegisSpawners.MoveNext(out var uid, out var aegisSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "aegisSpawner",
                    "AegisSpawner",
                    null,
                    null,
                    deleteAfterSpawn: aegisSpawner.DeleteAfterSpawn);
            }

            var aegisCorpseSpawners = entities.AllEntityQueryEnumerator<AegisCorpseSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (aegisCorpseSpawners.MoveNext(out var uid, out var aegisCorpseSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "aegisCorpseSpawner",
                    "AegisCorpseSpawner",
                    null,
                    null,
                    deleteAfterSpawn: aegisCorpseSpawner.DeleteAfterSpawn);
            }

            var proportionalSpawners = entities.AllEntityQueryEnumerator<ProportionalSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (proportionalSpawners.MoveNext(out var uid, out var proportionalSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "proportionalSpawner",
                    "ProportionalSpawner",
                    null,
                    null,
                    ratio: proportionalSpawner.Ratio,
                    targets: ToSortedStringList(proportionalSpawner.Prototypes.Select(proto => proto.ToString())));
            }

            var gridSpawners = entities.AllEntityQueryEnumerator<GridSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (gridSpawners.MoveNext(out var uid, out var gridSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "gridSpawner",
                    "GridSpawner",
                    null,
                    null,
                    spawnPath: gridSpawner.Spawn?.ToString());
            }

            var randomHumanoidSpawners = entities.AllEntityQueryEnumerator<RandomHumanoidSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (randomHumanoidSpawners.MoveNext(out var uid, out var humanoidSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "randomHumanoidSpawner",
                    "RandomHumanoidSpawner",
                    null,
                    null,
                    targetId: humanoidSpawner.SettingsPrototypeId);
            }

            var randomAnchoredSpawners = entities.AllEntityQueryEnumerator<RandomAnchoredSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (randomAnchoredSpawners.MoveNext(out var uid, out var anchoredSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "randomAnchoredSpawner",
                    "RandomAnchoredSpawner",
                    null,
                    null,
                    targetId: anchoredSpawner.Spawn?.ToString());
            }

            var ghostRoleSpawners = entities.AllEntityQueryEnumerator<GhostRoleMobSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (ghostRoleSpawners.MoveNext(out var uid, out var ghostRoleSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "ghostRoleSpawner",
                    "GhostRoleSpawner",
                    null,
                    null,
                    minCount: ghostRoleSpawner.AvailableTakeovers,
                    deleteAfterSpawn: ghostRoleSpawner.DeleteOnSpawn,
                    targetId: ghostRoleSpawner.Prototype?.ToString(),
                    targets: ToSortedStringList(ghostRoleSpawner.SelectablePrototypes));
            }

            var communicationsTowerSpawners = entities.AllEntityQueryEnumerator<CommunicationsTowerSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (communicationsTowerSpawners.MoveNext(out var uid, out var towerSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "communicationsTowerSpawner",
                    "CommunicationsTowerSpawner",
                    null,
                    null);
            }

            var squadSpawners = entities.AllEntityQueryEnumerator<SquadSpawnerComponent, MetaDataComponent, TransformComponent>();
            while (squadSpawners.MoveNext(out var uid, out var squadSpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "squadSpawner",
                    "SquadSpawner",
                    squadSpawner.Role?.ToString(),
                    null);
            }

            var deliverySpawners = entities.AllEntityQueryEnumerator<DeliverySpawnerComponent, MetaDataComponent, TransformComponent>();
            while (deliverySpawners.MoveNext(out var uid, out var deliverySpawner, out var meta, out var xform))
            {
                AddSpawn(
                    uid,
                    meta,
                    xform,
                    "deliverySpawner",
                    "DeliverySpawner",
                    null,
                    null,
                    maxCount: deliverySpawner.MaxContainedDeliveryAmount,
                    targetId: deliverySpawner.Table.GetType().Name);
            }

            var cells = new List<MapSpawnCell>(byTile.Count);
            foreach (var (indices, tileSpawns) in byTile)
            {
                tileSpawns.Sort(static (a, b) =>
                {
                    var kindCompare = string.Compare(a.Kind, b.Kind, StringComparison.OrdinalIgnoreCase);
                    return kindCompare != 0
                        ? kindCompare
                        : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });
                cells.Add(new MapSpawnCell(indices.X, indices.Y, tileSpawns));
            }

            cells.Sort(static (a, b) =>
            {
                var compareX = a.X.CompareTo(b.X);
                return compareX != 0 ? compareX : a.Y.CompareTo(b.Y);
            });

            return cells;

            void AddSpawn(
                EntityUid uid,
                MetaDataComponent meta,
                TransformComponent xform,
                string kind,
                string? spawnType,
                string? jobId,
                string? intelType,
                string? origin = "runtime",
                float? chance = null,
                float? rareChance = null,
                int? minCount = null,
                int? maxCount = null,
                int? quota = null,
                int? ratio = null,
                bool? deleteAfterSpawn = null,
                string? targetId = null,
                string? groupId = null,
                string? spawnPath = null,
                List<string>? targets = null,
                List<string>? rareTargets = null)
            {
                if (!seen.Add(uid) || xform.GridUid != gridUid)
                    return;

                var indices = mapSystem.CoordinatesToTile(gridUid, grid, xform.Coordinates);
                if (!IsWithinBounds(indices, renderBounds))
                    return;

                if (!byTile.TryGetValue(indices, out var tileSpawns))
                {
                    tileSpawns = new List<MapSpawnInfo>();
                    byTile[indices] = tileSpawns;
                }

                var prototypeId = meta.EntityPrototype?.ID;
                if (!string.IsNullOrWhiteSpace(prototypeId) && !_spawnIconEntities.ContainsKey(prototypeId))
                    _spawnIconEntities[prototypeId] = uid;

                tileSpawns.Add(new MapSpawnInfo
                {
                    Name = ResolveEntityDisplayName(meta, prototypes),
                    PrototypeId = prototypeId,
                    Kind = kind,
                    Origin = string.IsNullOrWhiteSpace(origin) ? null : origin,
                    SpawnType = string.IsNullOrWhiteSpace(spawnType) ? null : spawnType,
                    JobId = string.IsNullOrWhiteSpace(jobId) ? null : jobId,
                    IntelType = string.IsNullOrWhiteSpace(intelType) ? null : intelType,
                    Chance = chance,
                    RareChance = rareChance,
                    MinCount = minCount,
                    MaxCount = maxCount,
                    Quota = quota,
                    Ratio = ratio,
                    DeleteAfterSpawn = deleteAfterSpawn,
                    TargetId = string.IsNullOrWhiteSpace(targetId) ? null : targetId,
                    GroupId = string.IsNullOrWhiteSpace(groupId) ? null : groupId,
                    SpawnPath = string.IsNullOrWhiteSpace(spawnPath) ? null : spawnPath,
                    Targets = targets ?? new List<string>(),
                    RareTargets = rareTargets ?? new List<string>(),
                });
            }
        }

        private static List<MapTunnelCell> BuildTunnelCells(
            EntityUid gridUid,
            MapGridComponent grid,
            MapBounds renderBounds,
            SharedMapSystem mapSystem,
            IEntityManager entities,
            IPrototypeManager prototypes)
        {
            var byTile = new Dictionary<Vector2i, List<MapTunnelInfo>>();
            var query = entities.AllEntityQueryEnumerator<XenoTunnelComponent, MetaDataComponent, TransformComponent>();

            while (query.MoveNext(out _, out var tunnel, out var meta, out var xform))
            {
                if (xform.GridUid != gridUid)
                    continue;

                var indices = mapSystem.CoordinatesToTile(gridUid, grid, xform.Coordinates);
                if (!IsWithinBounds(indices, renderBounds))
                    continue;

                if (!byTile.TryGetValue(indices, out var tileTunnels))
                {
                    tileTunnels = new List<MapTunnelInfo>();
                    byTile[indices] = tileTunnels;
                }

                tileTunnels.Add(new MapTunnelInfo
                {
                    Name = ResolveEntityDisplayName(meta, prototypes),
                    PrototypeId = meta.EntityPrototype?.ID,
                    MaxMobs = tunnel.MaxMobs,
                    SmallXenoEnterDelay = tunnel.SmallXenoEnterDelay.TotalSeconds,
                    StandardXenoEnterDelay = tunnel.StandardXenoEnterDelay.TotalSeconds,
                    LargeXenoEnterDelay = tunnel.LargeXenoEnterDelay.TotalSeconds,
                    SmallXenoMoveDelay = tunnel.SmallXenoMoveDelay.TotalSeconds,
                    StandardXenoMoveDelay = tunnel.StandardXenoMoveDelay.TotalSeconds,
                    LargeXenoMoveDelay = tunnel.LargeXenoMoveDelay.TotalSeconds,
                });
            }

            var cells = new List<MapTunnelCell>(byTile.Count);
            foreach (var (indices, tileTunnels) in byTile)
            {
                tileTunnels.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                cells.Add(new MapTunnelCell(indices.X, indices.Y, tileTunnels));
            }

            cells.Sort(static (a, b) =>
            {
                var compareX = a.X.CompareTo(b.X);
                return compareX != 0 ? compareX : a.Y.CompareTo(b.Y);
            });

            return cells;
        }

        private static List<MapRoofingCell> BuildRoofingCells(
            EntityUid gridUid,
            MapGridComponent grid,
            MapBounds renderBounds,
            SharedMapSystem mapSystem,
            IEntityManager entities,
            IPrototypeManager prototypes)
        {
            var byTile = new Dictionary<Vector2i, List<MapRoofingInfo>>();
            var query = entities.AllEntityQueryEnumerator<RoofingEntityComponent, MetaDataComponent, TransformComponent>();

            while (query.MoveNext(out _, out var roofing, out var meta, out var xform))
            {
                if (xform.GridUid != gridUid)
                    continue;

                var indices = mapSystem.CoordinatesToTile(gridUid, grid, xform.Coordinates);
                if (!IsWithinBounds(indices, renderBounds))
                    continue;

                if (!byTile.TryGetValue(indices, out var tileRoofing))
                {
                    tileRoofing = new List<MapRoofingInfo>();
                    byTile[indices] = tileRoofing;
                }

                tileRoofing.Add(new MapRoofingInfo
                {
                    Name = ResolveEntityDisplayName(meta, prototypes),
                    PrototypeId = meta.EntityPrototype?.ID,
                    Range = roofing.Range,
                    Cas = roofing.CanCAS,
                    MortarPlacement = roofing.CanMortarPlace,
                    MortarFire = roofing.CanMortarFire,
                    Lasing = roofing.CanLase,
                    Medevac = roofing.CanMedevac,
                    Paradropping = roofing.CanParadrop,
                    OrbitalBombard = roofing.CanOrbitalBombard,
                    SupplyDrop = roofing.CanSupplyDrop,
                    Fulton = roofing.CanFulton,
                });
            }

            var cells = new List<MapRoofingCell>(byTile.Count);
            foreach (var (indices, tileRoofing) in byTile)
            {
                tileRoofing.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                cells.Add(new MapRoofingCell(indices.X, indices.Y, tileRoofing));
            }

            cells.Sort(static (a, b) =>
            {
                var compareX = a.X.CompareTo(b.X);
                return compareX != 0 ? compareX : a.Y.CompareTo(b.Y);
            });

            return cells;
        }

        private static (bool Exclude, uint Color) GetAreaRenderInfo(
            string areaId,
            IPrototypeManager prototypes,
            IComponentFactory componentFactory,
            Dictionary<string, (bool Exclude, uint Color)> cache)
        {
            if (cache.TryGetValue(areaId, out var info))
                return info;

            var exclude = false;
            var color = Robust.Shared.Maths.Color.FromHex("#6c6767d8");

            if (prototypes.TryIndex<EntityPrototype>(areaId, out var proto) &&
                proto.TryGetComponent(out AreaComponent? areaComp, componentFactory))
            {
                if (AreaExcludeFromTacMapRenderField?.GetValue(areaComp) is bool excludeValue)
                    exclude = excludeValue;

                if (AreaMinimapColorField?.GetValue(areaComp) is Robust.Shared.Maths.Color areaColor &&
                    areaColor != default)
                {
                    color = areaColor.WithAlpha(0.5f);
                }
            }

            info = (exclude, PackColor(color));
            cache[areaId] = info;
            return info;
        }

        private static string ResolveAreaName(EntityPrototype proto, IPrototypeManager prototypes)
        {
            var name = proto.Name;
            if (!string.IsNullOrWhiteSpace(name) &&
                !string.Equals(name, proto.ID, StringComparison.Ordinal))
            {
                return name;
            }

            var inheritedName = ResolveInheritedPrototypeName(proto, prototypes, new HashSet<string>(StringComparer.Ordinal));
            return string.IsNullOrWhiteSpace(inheritedName) ? proto.ID : inheritedName;
        }

        private static string ResolveInheritedPrototypeName(
            EntityPrototype proto,
            IPrototypeManager prototypes,
            HashSet<string> visited)
        {
            if (!visited.Add(proto.ID))
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(proto.SetName))
                return proto.Name;

            if (proto.Parents == null)
                return string.Empty;

            foreach (var parentId in proto.Parents)
            {
                if (!prototypes.TryIndex<EntityPrototype>(parentId, out var parent))
                    continue;

                var name = ResolveInheritedPrototypeName(parent, prototypes, visited);
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }

            return string.Empty;
        }

        private void MergeSourceMapSpawns(
            MapExport export,
            IEntityManager entities,
            IPrototypeManager prototypes,
            IComponentFactory componentFactory)
        {
            if (string.IsNullOrWhiteSpace(_sourceFilePath) || !File.Exists(_sourceFilePath))
                return;

            if (!TryReadSourceMapData(_sourceFilePath, out var sourceMap))
                return;

            var gridIdsByYamlUid = new Dictionary<int, string>();
            var gridQuery = entities.AllEntityQueryEnumerator<MapGridComponent, YamlUidComponent>();
            while (gridQuery.MoveNext(out var uid, out _, out var yamlUid))
            {
                if (_gridIds.TryGetValue(uid, out var gridId))
                    gridIdsByYamlUid[yamlUid.Uid] = gridId;
            }

            if (gridIdsByYamlUid.Count == 0)
                return;

            var exportGrids = export.Grids.ToDictionary(grid => grid.GridId, StringComparer.Ordinal);
            var positionCache = new Dictionary<int, (int? GridYamlUid, Vector2 Position)>();

            foreach (var entity in sourceMap.Entities.Values)
            {
                if (string.IsNullOrWhiteSpace(entity.PrototypeId))
                    continue;

                if (!TryResolveSourceGridPosition(entity.YamlUid, sourceMap, positionCache, out var sourceGridYamlUid, out var sourcePosition) ||
                    sourceGridYamlUid == null ||
                    !gridIdsByYamlUid.TryGetValue(sourceGridYamlUid.Value, out var gridId) ||
                    !exportGrids.TryGetValue(gridId, out var gridExport))
                {
                    continue;
                }

                var tile = new Vector2i(
                    (int) MathF.Floor(sourcePosition.X),
                    (int) MathF.Floor(sourcePosition.Y));

                if (!IsWithinBounds(tile, gridExport.RenderBounds))
                    continue;

                var spawn = TryBuildSpawnInfoFromPrototype(entity.PrototypeId!, prototypes, componentFactory, "sourceMap");
                if (spawn == null)
                    continue;

                if (spawn.PrototypeId != null)
                    _spawnIconPrototypeIds.Add(spawn.PrototypeId);

                var indexed = IndexSpawnCells(gridExport.Spawns);
                AddSpawnInfo(indexed, tile, spawn);
                gridExport.Spawns = BuildSpawnCells(indexed);
            }
        }

        private static Dictionary<Vector2i, List<MapSpawnInfo>> IndexSpawnCells(IEnumerable<MapSpawnCell> cells)
        {
            var byTile = new Dictionary<Vector2i, List<MapSpawnInfo>>();
            foreach (var cell in cells)
            {
                byTile[new Vector2i(cell.X, cell.Y)] = new List<MapSpawnInfo>(cell.Spawns);
            }

            return byTile;
        }

        private static List<MapSpawnCell> BuildSpawnCells(Dictionary<Vector2i, List<MapSpawnInfo>> byTile)
        {
            var cells = new List<MapSpawnCell>(byTile.Count);
            foreach (var (indices, tileSpawns) in byTile)
            {
                tileSpawns.Sort(static (a, b) =>
                {
                    var kindCompare = string.Compare(a.Kind, b.Kind, StringComparison.OrdinalIgnoreCase);
                    return kindCompare != 0
                        ? kindCompare
                        : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });

                cells.Add(new MapSpawnCell(indices.X, indices.Y, tileSpawns));
            }

            cells.Sort(static (a, b) =>
            {
                var compareX = a.X.CompareTo(b.X);
                return compareX != 0 ? compareX : a.Y.CompareTo(b.Y);
            });

            return cells;
        }

        private static void AddSpawnInfo(
            Dictionary<Vector2i, List<MapSpawnInfo>> byTile,
            Vector2i indices,
            MapSpawnInfo spawn)
        {
            if (!byTile.TryGetValue(indices, out var tileSpawns))
            {
                tileSpawns = new List<MapSpawnInfo>();
                byTile[indices] = tileSpawns;
            }

            if (tileSpawns.Any(existing =>
                    string.Equals(existing.PrototypeId, spawn.PrototypeId, StringComparison.Ordinal) &&
                    string.Equals(existing.Kind, spawn.Kind, StringComparison.Ordinal) &&
                    string.Equals(existing.SpawnType, spawn.SpawnType, StringComparison.Ordinal) &&
                    string.Equals(existing.JobId, spawn.JobId, StringComparison.Ordinal) &&
                    string.Equals(existing.IntelType, spawn.IntelType, StringComparison.Ordinal)))
            {
                return;
            }

            tileSpawns.Add(spawn);
        }

        private static bool TryReadSourceMapData(string filePath, out SourceMapData sourceMap)
        {
            sourceMap = new SourceMapData();

            using var reader = File.OpenText(filePath);
            var document = DataNodeParser.ParseYamlStream(reader).FirstOrDefault();
            if (document?.Root is not MappingDataNode root ||
                !root.TryGet<SequenceDataNode>("entities", out var entityGroups))
            {
                return false;
            }

            foreach (var groupNode in entityGroups)
            {
                if (groupNode is not MappingDataNode group)
                    continue;

                var prototypeId = group.TryGet<ValueDataNode>("proto", out var protoNode)
                    ? protoNode.Value
                    : string.Empty;

                if (!group.TryGet<SequenceDataNode>("entities", out var entitiesNode))
                    continue;

                foreach (var entityNode in entitiesNode)
                {
                    if (entityNode is not MappingDataNode entityMapping ||
                        !entityMapping.TryGet<ValueDataNode>("uid", out var uidNode) ||
                        !int.TryParse(uidNode.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var yamlUid))
                    {
                        continue;
                    }

                    var sourceEntity = new SourceEntityData
                    {
                        YamlUid = yamlUid,
                        PrototypeId = string.IsNullOrWhiteSpace(prototypeId) ? null : prototypeId,
                    };

                    if (entityMapping.TryGet<SequenceDataNode>("components", out var componentsNode))
                    {
                        foreach (var componentNode in componentsNode)
                        {
                            if (componentNode is not MappingDataNode componentMapping ||
                                !componentMapping.TryGet<ValueDataNode>("type", out var typeNode))
                            {
                                continue;
                            }

                            switch (typeNode.Value)
                            {
                                case "Transform":
                                    if (componentMapping.TryGet<ValueDataNode>("parent", out var parentNode) &&
                                        int.TryParse(parentNode.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parentUid))
                                    {
                                        sourceEntity.ParentYamlUid = parentUid;
                                    }

                                    if (componentMapping.TryGet<ValueDataNode>("pos", out var posNode) &&
                                        TryParseVector2(posNode.Value, out var localPos))
                                    {
                                        sourceEntity.LocalPosition = localPos;
                                    }

                                    break;
                                case "MapGrid":
                                    sourceEntity.IsGrid = true;
                                    break;
                            }
                        }
                    }

                    sourceMap.Entities[yamlUid] = sourceEntity;
                }
            }

            return sourceMap.Entities.Count > 0;
        }

        private static bool TryResolveSourceGridPosition(
            int yamlUid,
            SourceMapData sourceMap,
            Dictionary<int, (int? GridYamlUid, Vector2 Position)> cache,
            out int? gridYamlUid,
            out Vector2 position)
        {
            if (cache.TryGetValue(yamlUid, out var cached))
            {
                gridYamlUid = cached.GridYamlUid;
                position = cached.Position;
                return gridYamlUid != null;
            }

            if (!sourceMap.Entities.TryGetValue(yamlUid, out var entity))
            {
                gridYamlUid = null;
                position = default;
                return false;
            }

            if (entity.IsGrid)
            {
                gridYamlUid = entity.YamlUid;
                position = Vector2.Zero;
                cache[yamlUid] = (gridYamlUid, position);
                return true;
            }

            if (entity.ParentYamlUid == null)
            {
                gridYamlUid = null;
                position = entity.LocalPosition;
                cache[yamlUid] = (gridYamlUid, position);
                return false;
            }

            if (!TryResolveSourceGridPosition(entity.ParentYamlUid.Value, sourceMap, cache, out gridYamlUid, out position))
            {
                position += entity.LocalPosition;
                cache[yamlUid] = (gridYamlUid, position);
                return false;
            }

            position += entity.LocalPosition;
            cache[yamlUid] = (gridYamlUid, position);
            return true;
        }

        private static bool TryParseVector2(string value, out Vector2 vector)
        {
            vector = default;
            var parts = value.Split(',', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 ||
                !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            {
                return false;
            }

            vector = new Vector2(x, y);
            return true;
        }

        private static MapSpawnInfo? TryBuildSpawnInfoFromPrototype(
            string prototypeId,
            IPrototypeManager prototypes,
            IComponentFactory componentFactory,
            string origin)
        {
            if (!prototypes.TryIndex<EntityPrototype>(prototypeId, out var prototype))
                return null;

            if (prototype.TryGetComponent(out SpawnPointComponent? spawnPoint, componentFactory))
            {
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    spawnPoint.SpawnType switch
                    {
                        SpawnPointType.Job => "job",
                        SpawnPointType.LateJoin => "latejoin",
                        SpawnPointType.Observer => "observer",
                        _ => "spawn",
                    },
                    origin,
                    spawnType: FormatSpawnPointType(spawnPoint.SpawnType),
                    jobId: spawnPoint.Job?.ToString());
            }

            if (prototype.TryGetComponent(out RMCJobSpawnerComponent? rmcJobSpawner, componentFactory))
                return NewSpawnInfo(prototype, prototypes, "job", origin, spawnType: "Job", jobId: rmcJobSpawner.Job?.ToString());

            if (prototype.TryGetComponent(out XenoSpawnPointComponent? _, componentFactory))
                return NewSpawnInfo(prototype, prototypes, "xeno", origin, spawnType: "Xeno");

            if (prototype.TryGetComponent(out XenoLeaderSpawnPointComponent? _, componentFactory))
                return NewSpawnInfo(prototype, prototypes, "xenoLeader", origin, spawnType: "XenoLeader");

            if (prototype.TryGetComponent(out IntelSpawnerComponent? intelSpawner, componentFactory))
                return NewSpawnInfo(prototype, prototypes, "intel", origin, spawnType: "Intel", intelType: FormatIntelSpawnerType(intelSpawner.IntelType));

            if (prototype.TryGetComponent(out GunSpawnerComponent? gunSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "gunSpawner",
                    origin,
                    spawnType: "GunSpawner",
                    chance: gunSpawner.ChanceToSpawn,
                    minCount: gunSpawner.MinMagazines,
                    maxCount: gunSpawner.MaxMagazines,
                    deleteAfterSpawn: gunSpawner.DeleteAfterSpawn,
                    targets: ToSortedStringList(gunSpawner.Prototypes.Select(entry => entry.Gun.ToString())));

            if (prototype.TryGetComponent(out UniqueRandomSpawnerComponent? uniqueRandomSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "uniqueRandomSpawner",
                    origin,
                    spawnType: "UniqueRandomSpawner",
                    chance: uniqueRandomSpawner.Chance,
                    deleteAfterSpawn: uniqueRandomSpawner.DeleteSpawnerAfterSpawn,
                    groupId: uniqueRandomSpawner.SpawnerGroup.ToString(),
                    targets: ToSortedStringList(uniqueRandomSpawner.Prototypes.Select(entry => entry.ToString())));

            if (prototype.TryGetComponent(out RandomSpawnerComponent? randomSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "randomSpawner",
                    origin,
                    spawnType: "RandomSpawner",
                    chance: randomSpawner.Chance,
                    rareChance: randomSpawner.RareChance,
                    deleteAfterSpawn: randomSpawner.DeleteSpawnerAfterSpawn,
                    targets: ToSortedStringList(randomSpawner.Prototypes.Select(entry => entry.ToString())),
                    rareTargets: ToSortedStringList(randomSpawner.RarePrototypes.Select(entry => entry.ToString())));

            if (prototype.TryGetComponent(out ConditionalSpawnerComponent? conditionalSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "conditionalSpawner",
                    origin,
                    spawnType: "ConditionalSpawner",
                    chance: conditionalSpawner.Chance,
                    targets: ToSortedStringList(conditionalSpawner.Prototypes.Select(entry => entry.ToString())));

            if (prototype.TryGetComponent(out EntityTableSpawnerComponent? entityTableSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "entityTableSpawner",
                    origin,
                    spawnType: "EntityTableSpawner",
                    deleteAfterSpawn: entityTableSpawner.DeleteSpawnerAfterSpawn);

            if (prototype.TryGetComponent(out ItemPoolSpawnerComponent? itemPoolSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "itemPoolSpawner",
                    origin,
                    spawnType: "ItemPoolSpawner",
                    quota: itemPoolSpawner.Quota,
                    targets: ToSortedStringList(itemPoolSpawner.Prototypes.Select(entry => entry.ToString())));

            if (prototype.TryGetComponent(out CorpseSpawnerComponent? corpseSpawner, componentFactory))
                return NewSpawnInfo(prototype, prototypes, "corpseSpawner", origin, spawnType: "CorpseSpawner", targetId: corpseSpawner.Spawn?.ToString());

            if (prototype.TryGetComponent(out AegisSpawnerComponent? aegisSpawner, componentFactory))
                return NewSpawnInfo(prototype, prototypes, "aegisSpawner", origin, spawnType: "AegisSpawner", deleteAfterSpawn: aegisSpawner.DeleteAfterSpawn);

            if (prototype.TryGetComponent(out AegisCorpseSpawnerComponent? aegisCorpseSpawner, componentFactory))
                return NewSpawnInfo(prototype, prototypes, "aegisCorpseSpawner", origin, spawnType: "AegisCorpseSpawner", deleteAfterSpawn: aegisCorpseSpawner.DeleteAfterSpawn);

            if (prototype.TryGetComponent(out ProportionalSpawnerComponent? proportionalSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "proportionalSpawner",
                    origin,
                    spawnType: "ProportionalSpawner",
                    ratio: proportionalSpawner.Ratio,
                    targets: ToSortedStringList(proportionalSpawner.Prototypes.Select(entry => entry.ToString())));

            if (prototype.TryGetComponent(out GridSpawnerComponent? gridSpawner, componentFactory))
                return NewSpawnInfo(prototype, prototypes, "gridSpawner", origin, spawnType: "GridSpawner", spawnPath: gridSpawner.Spawn?.ToString());

            if (prototype.TryGetComponent(out RandomHumanoidSpawnerComponent? humanoidSpawner, componentFactory))
                return NewSpawnInfo(prototype, prototypes, "randomHumanoidSpawner", origin, spawnType: "RandomHumanoidSpawner", targetId: humanoidSpawner.SettingsPrototypeId);

            if (prototype.TryGetComponent(out RandomAnchoredSpawnerComponent? randomAnchoredSpawner, componentFactory))
                return NewSpawnInfo(prototype, prototypes, "randomAnchoredSpawner", origin, spawnType: "RandomAnchoredSpawner", targetId: randomAnchoredSpawner.Spawn?.ToString());

            if (prototype.TryGetComponent(out GhostRoleMobSpawnerComponent? ghostRoleSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "ghostRoleSpawner",
                    origin,
                    spawnType: "GhostRoleSpawner",
                    minCount: ghostRoleSpawner.AvailableTakeovers,
                    deleteAfterSpawn: ghostRoleSpawner.DeleteOnSpawn,
                    targetId: ghostRoleSpawner.Prototype?.ToString(),
                    targets: ToSortedStringList(ghostRoleSpawner.SelectablePrototypes));

            if (prototype.TryGetComponent(out CommunicationsTowerSpawnerComponent? communicationsTowerSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "communicationsTowerSpawner",
                    origin,
                    spawnType: "CommunicationsTowerSpawner");

            if (prototype.TryGetComponent(out SquadSpawnerComponent? squadSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "squadSpawner",
                    origin,
                    spawnType: "SquadSpawner",
                    jobId: squadSpawner.Role?.ToString());

            if (prototype.TryGetComponent(out DeliverySpawnerComponent? deliverySpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "deliverySpawner",
                    origin,
                    spawnType: "DeliverySpawner",
                    maxCount: deliverySpawner.MaxContainedDeliveryAmount,
                    targetId: deliverySpawner.Table.GetType().Name);

            if (prototype.TryGetComponent(out TimedSpawnerComponent? timedSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "timedSpawner",
                    origin,
                    spawnType: "TimedSpawner",
                    chance: timedSpawner.Chance,
                    minCount: timedSpawner.MinimumEntitiesSpawned,
                    maxCount: timedSpawner.MaximumEntitiesSpawned,
                    targets: ToSortedStringList(timedSpawner.Prototypes.Select(entry => entry.ToString())));

            if (prototype.TryGetComponent(out RandomCloneSpawnerComponent? randomCloneSpawner, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "randomCloneSpawner",
                    origin,
                    spawnType: "RandomCloneSpawner",
                    targetId: randomCloneSpawner.Settings.ToString());

            if (prototype.TryGetComponent(out RandomPatronFigurineSpawnerComponent? _, componentFactory))
                return NewSpawnInfo(
                    prototype,
                    prototypes,
                    "randomPatronFigurineSpawner",
                    origin,
                    spawnType: "RandomPatronFigurineSpawner");

            return null;
        }

        private static MapSpawnInfo NewSpawnInfo(
            EntityPrototype prototype,
            IPrototypeManager prototypes,
            string kind,
            string origin,
            string? spawnType = null,
            string? jobId = null,
            string? intelType = null,
            float? chance = null,
            float? rareChance = null,
            int? minCount = null,
            int? maxCount = null,
            int? quota = null,
            int? ratio = null,
            bool? deleteAfterSpawn = null,
            string? targetId = null,
            string? groupId = null,
            string? spawnPath = null,
            List<string>? targets = null,
            List<string>? rareTargets = null)
        {
            return new MapSpawnInfo
            {
                Name = ResolveAreaName(prototype, prototypes),
                PrototypeId = prototype.ID,
                Kind = kind,
                Origin = origin,
                SpawnType = string.IsNullOrWhiteSpace(spawnType) ? null : spawnType,
                JobId = string.IsNullOrWhiteSpace(jobId) ? null : jobId,
                IntelType = string.IsNullOrWhiteSpace(intelType) ? null : intelType,
                Chance = chance,
                RareChance = rareChance,
                MinCount = minCount,
                MaxCount = maxCount,
                Quota = quota,
                Ratio = ratio,
                DeleteAfterSpawn = deleteAfterSpawn,
                TargetId = string.IsNullOrWhiteSpace(targetId) ? null : targetId,
                GroupId = string.IsNullOrWhiteSpace(groupId) ? null : groupId,
                SpawnPath = string.IsNullOrWhiteSpace(spawnPath) ? null : spawnPath,
                Targets = targets ?? new List<string>(),
                RareTargets = rareTargets ?? new List<string>(),
            };
        }

        private static string ResolveEntityDisplayName(MetaDataComponent meta, IPrototypeManager prototypes)
        {
            if (!string.IsNullOrWhiteSpace(meta.EntityName))
                return meta.EntityName;

            if (meta.EntityPrototype is { } prototype)
                return ResolveAreaName(prototype, prototypes);

            return string.Empty;
        }

        private static List<string> ToSortedStringList(IEnumerable<string?> values)
        {
            return values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static uint PackColor(Robust.Shared.Maths.Color color)
        {
            return ((uint) color.RByte << 24) |
                   ((uint) color.GByte << 16) |
                   ((uint) color.BByte << 8) |
                   color.AByte;
        }

        private static bool HasTacMapBounds(AreaGridComponent grid)
        {
            return AreaGridHasTacMapBoundsField?.GetValue(grid) as bool? ?? false;
        }

        private static bool TryGetTacMapBounds(AreaGridComponent grid, out Vector2i min, out Vector2i max)
        {
            min = default;
            max = default;

            if (!HasTacMapBounds(grid))
                return false;

            if (AreaGridTacMapBoundsMinField?.GetValue(grid) is not Vector2i boundsMin ||
                AreaGridTacMapBoundsMaxField?.GetValue(grid) is not Vector2i boundsMax)
            {
                return false;
            }

            min = boundsMin;
            max = boundsMax;
            return true;
        }

        private static string FormatSpawnPointType(SpawnPointType spawnType)
        {
            return spawnType switch
            {
                SpawnPointType.Job => "Job",
                SpawnPointType.LateJoin => "LateJoin",
                SpawnPointType.Observer => "Observer",
                _ => "Unset",
            };
        }

        private static string FormatIntelSpawnerType(IntelSpawnerType intelType)
        {
            return intelType switch
            {
                IntelSpawnerType.Close => "Close",
                IntelSpawnerType.Medium => "Medium",
                IntelSpawnerType.Far => "Far",
                IntelSpawnerType.Science => "Science",
                _ => "Unknown",
            };
        }

        private static string FormatHijackEvacuationType(AreaHijackEvacuationType hijackType)
        {
            return hijackType switch
            {
                AreaHijackEvacuationType.Add => "Add",
                AreaHijackEvacuationType.Multiply => "Multiply",
                _ => "None",
            };
        }

        private async Task BuildSpawnIcons()
        {
            if (_pair == null || (_spawnIconEntities.Count == 0 && _spawnIconPrototypeIds.Count == 0))
                return;

            _spawnIcons.Clear();

            var serverEntities = _pair.Server.ResolveDependency<IEntityManager>();
            var serverPlayers = _pair.Server.ResolveDependency<IPlayerManager>();
            var clientEntities = _pair.Client.ResolveDependency<IEntityManager>();
            var markerSystem = _pair.Client.System<MarkerSystem>();
            var entityPainter = new EntityPainter(_pair.Client, _pair.Server);
            var temporaryEntities = new Dictionary<string, EntityUid>(StringComparer.Ordinal);

            var missingPrototypeIcons = _spawnIconPrototypeIds
                .Where(prototypeId => !_spawnIconEntities.ContainsKey(prototypeId))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (missingPrototypeIcons.Count > 0)
            {
                await _pair.Server.WaitPost(() =>
                {
                    var player = serverPlayers.Sessions.Single().AttachedEntity;
                    if (!player.HasValue)
                        return;

                    var coordinates = serverEntities.GetComponent<TransformComponent>(player.Value).Coordinates;

                    foreach (var prototypeId in missingPrototypeIcons)
                    {
                        if (temporaryEntities.ContainsKey(prototypeId))
                            continue;

                        temporaryEntities[prototypeId] = serverEntities.SpawnEntity(prototypeId, coordinates);
                    }
                });

                await _pair.RunTicksSync(2);
                await Task.WhenAll(_pair.Client.WaitIdleAsync(), _pair.Server.WaitIdleAsync());
            }

            await _pair.Client.WaitPost(() =>
            {
                var markersVisible = markerSystem.MarkersVisible;
                markerSystem.MarkersVisible = true;

                try
                {
                    foreach (var (prototypeId, serverUid) in _spawnIconEntities)
                    {
                        if (_spawnIcons.ContainsKey(prototypeId))
                            continue;

                        var netEntity = serverEntities.GetNetEntity(serverUid);
                        var clientUid = clientEntities.GetEntity(netEntity);

                        if (!clientEntities.TryGetComponent(clientUid, out SpriteComponent? sprite))
                            continue;

                        var icon = entityPainter.RenderIcon(new EntityData(serverUid, sprite, 0, 0));
                        if (icon == null)
                            continue;

                        _spawnIcons[prototypeId] = icon;
                    }

                    foreach (var (prototypeId, serverUid) in temporaryEntities)
                    {
                        if (_spawnIcons.ContainsKey(prototypeId))
                            continue;

                        var netEntity = serverEntities.GetNetEntity(serverUid);
                        var clientUid = clientEntities.GetEntity(netEntity);

                        if (!clientEntities.TryGetComponent(clientUid, out SpriteComponent? sprite))
                            continue;

                        var icon = entityPainter.RenderIcon(new EntityData(serverUid, sprite, 0, 0));
                        if (icon == null)
                            continue;

                        _spawnIcons[prototypeId] = icon;
                    }
                }
                finally
                {
                    markerSystem.MarkersVisible = markersVisible;
                }
            });

            if (temporaryEntities.Count > 0)
            {
                await _pair.Server.WaitPost(() =>
                {
                    foreach (var uid in temporaryEntities.Values)
                    {
                        if (serverEntities.EntityExists(uid))
                            serverEntities.DeleteEntity(uid);
                    }
                });

                await _pair.RunTicksSync(1);
            }
        }
    }
}
