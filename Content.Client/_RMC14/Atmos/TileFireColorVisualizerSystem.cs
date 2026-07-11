using Content.Shared._RMC14.Atmos;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Atmos;

public sealed class TileFireColorVisualizerSystem : VisualizerSystem<TileFireComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, TileFireComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!TryComp<RMCFireColorComponent>(uid, out var fireColor))
            return;

        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), TileFireLayers.Base, out var index, false))
            return;

        SpriteSystem.LayerSetColor((uid, args.Sprite), index, fireColor.Color);
    }
}
