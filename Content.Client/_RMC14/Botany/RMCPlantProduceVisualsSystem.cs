using System.Numerics;
using Content.Shared._RMC14.Botany;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Botany;

public sealed class RMCPlantProduceVisualsSystem : VisualizerSystem<RMCPlantProduceVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RMCPlantProduceVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<float>(uid, RMCProduceVisuals.Potency, out var potency, args.Component))
        {
            var scale = MathHelper.Lerp(component.MinimumScale, component.MaximumScale, potency / 100);
            SpriteSystem.SetScale((uid, args.Sprite), new Vector2(scale, scale));
        }

        if (AppearanceSystem.TryGetData<Color>(uid, RMCProduceVisuals.Color, out var color, args.Component))
            SpriteSystem.SetColor((uid, args.Sprite), color);
    }
}
