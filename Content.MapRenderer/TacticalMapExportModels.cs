using System;
using System.Collections.Generic;

namespace Content.MapRenderer;

public sealed class MapExportManifest
{
    public DateTime GeneratedUtc { get; set; }
    public List<MapExportManifestMap> Maps { get; set; } = new();
}

public sealed class MapExportManifestMap
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
}

public sealed class MapExport
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<MapExportGrid> Grids { get; set; } = new();
}

public sealed class MapExportGrid
{
    public string GridId { get; set; } = string.Empty;
    public bool HasMapBounds { get; set; }
    public MapBounds Bounds { get; set; } = new(0, 0, 0, 0);
    public MapBounds RenderBounds { get; set; } = new(0, 0, 0, 0);
    public MapRenderImage? Image { get; set; }
    public List<MapColorCell> Colors { get; set; } = new();
    public List<MapAreaCell> Areas { get; set; } = new();
    public List<MapLabelCell> Labels { get; set; } = new();
    public List<MapEntityCell> Entities { get; set; } = new();
    public List<MapInsertCell> Inserts { get; set; } = new();
    public List<MapSpawnCell> Spawns { get; set; } = new();
    public List<MapTunnelCell> Tunnels { get; set; } = new();
    public List<MapRoofingCell> Roofing { get; set; } = new();
    public List<string> AreaIds { get; set; } = new();
    public List<MapAreaInfo> AreaInfo { get; set; } = new();
}

public readonly record struct MapBounds(int MinX, int MinY, int MaxX, int MaxY);
public readonly record struct MapColorCell(int X, int Y, uint C);
public readonly record struct MapAreaCell(int X, int Y, int A);
public readonly record struct MapLabelCell(int X, int Y, string T);
public readonly record struct MapEntityCell(int X, int Y, List<MapEntityInfo> Entities);
public readonly record struct MapInsertCell(int X, int Y, List<MapInsertInfo> Inserts);
public readonly record struct MapSpawnCell(int X, int Y, List<MapSpawnInfo> Spawns);
public readonly record struct MapTunnelCell(int X, int Y, List<MapTunnelInfo> Tunnels);
public readonly record struct MapRoofingCell(int X, int Y, List<MapRoofingInfo> Roofing);

public sealed class MapRenderImage
{
    public string File { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int PixelsPerTile { get; set; }
}

public sealed class MapEntityInfo
{
    public string Name { get; set; } = string.Empty;
    public string? PrototypeId { get; set; }
}

public sealed class MapInsertInfo
{
    public string Name { get; set; } = string.Empty;
    public string? PrototypeId { get; set; }
    public bool ClearEntities { get; set; }
    public bool ClearDecals { get; set; }
    public bool ReplaceAreas { get; set; }
    public List<MapInsertVariationInfo> Variations { get; set; } = new();
}

public sealed class MapInsertVariationInfo
{
    public string Spawn { get; set; } = string.Empty;
    public float Probability { get; set; }
    public string? NightmareScenario { get; set; }
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public string? Overlay { get; set; }
    public List<MapSpawnCell> Spawns { get; set; } = new();
}

public sealed class MapSpawnInfo
{
    public string Name { get; set; } = string.Empty;
    public string? PrototypeId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string? Origin { get; set; }
    public string? SpawnType { get; set; }
    public string? JobId { get; set; }
    public string? IntelType { get; set; }
    public float? Chance { get; set; }
    public float? RareChance { get; set; }
    public int? MinCount { get; set; }
    public int? MaxCount { get; set; }
    public int? Quota { get; set; }
    public int? Ratio { get; set; }
    public bool? DeleteAfterSpawn { get; set; }
    public string? TargetId { get; set; }
    public string? GroupId { get; set; }
    public string? SpawnPath { get; set; }
    public List<string> Targets { get; set; } = new();
    public List<string> RareTargets { get; set; } = new();
    public string? Icon { get; set; }
}

public sealed class MapTunnelInfo
{
    public string Name { get; set; } = string.Empty;
    public string? PrototypeId { get; set; }
    public int MaxMobs { get; set; }
    public double SmallXenoEnterDelay { get; set; }
    public double StandardXenoEnterDelay { get; set; }
    public double LargeXenoEnterDelay { get; set; }
    public double SmallXenoMoveDelay { get; set; }
    public double StandardXenoMoveDelay { get; set; }
    public double LargeXenoMoveDelay { get; set; }
}

public sealed class MapRoofingInfo
{
    public string Name { get; set; } = string.Empty;
    public string? PrototypeId { get; set; }
    public float Range { get; set; }
    public bool Cas { get; set; }
    public bool MortarPlacement { get; set; }
    public bool MortarFire { get; set; }
    public bool Lasing { get; set; }
    public bool Medevac { get; set; }
    public bool Paradropping { get; set; }
    public bool OrbitalBombard { get; set; }
    public bool SupplyDrop { get; set; }
    public bool Fulton { get; set; }
}

public sealed class MapAreaInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Cas { get; set; }
    public bool MortarFire { get; set; }
    public bool MortarPlacement { get; set; }
    public bool Lasing { get; set; }
    public bool Medevac { get; set; }
    public bool Paradropping { get; set; }
    public bool OrbitalBombard { get; set; }
    public bool SupplyDrop { get; set; }
    public bool Fulton { get; set; }
    public bool AvoidBioscan { get; set; }
    public bool NoTunnel { get; set; }
    public bool Unweedable { get; set; }
    public bool BuildSpecial { get; set; }
    public bool ResinAllowed { get; set; }
    public bool ResinConstructionAllowed { get; set; }
    public bool WeatherEnabled { get; set; }
    public bool HijackEvacuationArea { get; set; }
    public bool AlwaysPowered { get; set; }
    public double HijackEvacuationWeight { get; set; }
    public string HijackEvacuationType { get; set; } = string.Empty;
    public string? PowerNet { get; set; }
    public uint MinimapColor { get; set; }
    public int ZLevel { get; set; }
    public bool LandingZone { get; set; }
    public string? LinkedLz { get; set; }
    public bool WeedKilling { get; set; }
    public bool RetrieveItemObjective { get; set; }
    public int BuildableTiles { get; set; }
    public int ResinConstructCount { get; set; }
    public bool ExcludeFromTacMapRender { get; set; }
}
