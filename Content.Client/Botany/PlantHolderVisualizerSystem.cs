using Content.Client.Botany.Components;
using Content.Shared.Botany;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Botany;

public sealed class PlantHolderVisualizerSystem : VisualizerSystem<PlantHolderVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantHolderVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, PlantHolderVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        SpriteSystem.LayerMapReserve((uid, sprite), PlantHolderLayers.Plant);
        SpriteSystem.LayerSetVisible((uid, sprite), PlantHolderLayers.Plant, false);

        SpriteSystem.LayerMapReserve((uid, sprite), PlantHolderLayers.Flower);
        SpriteSystem.LayerSetVisible((uid, sprite), PlantHolderLayers.Flower, false);
    }

    protected override void OnAppearanceChange(EntityUid uid, PlantHolderVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, PlantHolderVisuals.PlantRsi, out var rsi, args.Component)
            && AppearanceSystem.TryGetData<string>(uid, PlantHolderVisuals.PlantState, out var state, args.Component))
        {
            var valid = !string.IsNullOrWhiteSpace(state);

            SpriteSystem.LayerSetVisible((uid, args.Sprite), PlantHolderLayers.Plant, valid);

            if (valid)
            {
                SpriteSystem.LayerSetRsi((uid, args.Sprite), PlantHolderLayers.Plant, new ResPath(rsi));
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), PlantHolderLayers.Plant, state);
            }
        }

        if (AppearanceSystem.TryGetData<string>(uid, PlantHolderVisuals.FlowerRsi, out var flowerRsi, args.Component)
            && AppearanceSystem.TryGetData<string>(uid, PlantHolderVisuals.FlowerState, out var flowerState, args.Component))
        {
            var flowerValid = !string.IsNullOrWhiteSpace(flowerState);

            SpriteSystem.LayerSetVisible((uid, args.Sprite), PlantHolderLayers.Flower, flowerValid);

            if (flowerValid)
            {
                SpriteSystem.LayerSetRsi((uid, args.Sprite), PlantHolderLayers.Flower, new ResPath(flowerRsi));
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), PlantHolderLayers.Flower, flowerState);

                var color = AppearanceSystem.TryGetData<Color>(uid, PlantHolderVisuals.FlowerColor, out var flowerColor, args.Component)
                    ? flowerColor
                    : Color.White;
                SpriteSystem.LayerSetColor((uid, args.Sprite), PlantHolderLayers.Flower, color);
            }
        }
    }
}

public enum PlantHolderLayers : byte
{
    Plant,
    Flower,
    HealthLight,
    WaterLight,
    NutritionLight,
    AlertLight,
    HarvestLight,
}
