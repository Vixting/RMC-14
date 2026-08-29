using Content.Shared._RMC14.Botany;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Botany;

public enum RMCPlantLayers : byte
{
    Plant,
    Flower,
}

public sealed class RMCPlantVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCPlantComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<RMCPlantComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.LayerMapReserve((ent, sprite), RMCPlantLayers.Plant);
        _sprite.LayerSetVisible((ent, sprite), RMCPlantLayers.Plant, false);

        _sprite.LayerMapReserve((ent, sprite), RMCPlantLayers.Flower);
        _sprite.LayerSetVisible((ent, sprite), RMCPlantLayers.Flower, false);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RMCPlantComponent, RMCPlantGrowthComponent, RMCPlantTraitsComponent>();
        while (query.MoveNext(out var uid, out var plant, out var growth, out var traits))
        {
            if (!TryComp(uid, out SpriteComponent? sprite))
                continue;

            UpdateSprite(uid, plant, growth, traits, sprite);
        }
    }

    private void UpdateSprite(
        EntityUid uid,
        RMCPlantComponent plant,
        RMCPlantGrowthComponent growth,
        RMCPlantTraitsComponent traits,
        SpriteComponent sprite)
    {
        var state = plant.Dead
            ? "dead"
            : plant.Harvest
                ? "harvest"
                : plant.Age < growth.Maturation
                    ? $"stage-{GetCurrentGrowthStage(plant, growth)}"
                    : $"stage-{growth.GrowthStages}";

        _sprite.LayerSetVisible((uid, sprite), RMCPlantLayers.Plant, true);
        _sprite.LayerSetRsi((uid, sprite), RMCPlantLayers.Plant, traits.PlantRsi);
        _sprite.LayerSetRsiState((uid, sprite), RMCPlantLayers.Plant, state);

        var flowerValid = !plant.Dead && traits.Flowers && !string.IsNullOrEmpty(traits.FlowerIcon);
        _sprite.LayerSetVisible((uid, sprite), RMCPlantLayers.Flower, flowerValid);

        if (flowerValid)
        {
            _sprite.LayerSetRsi((uid, sprite), RMCPlantLayers.Flower, traits.PlantRsi);
            _sprite.LayerSetRsiState((uid, sprite), RMCPlantLayers.Flower, traits.FlowerIcon!);
            _sprite.LayerSetColor((uid, sprite), RMCPlantLayers.Flower, traits.FlowerColor ?? Color.White);
        }
    }

    private static int GetCurrentGrowthStage(RMCPlantComponent plant, RMCPlantGrowthComponent growth)
    {
        return Math.Max(1, (int)(plant.Age * growth.GrowthStages / growth.Maturation));
    }
}
