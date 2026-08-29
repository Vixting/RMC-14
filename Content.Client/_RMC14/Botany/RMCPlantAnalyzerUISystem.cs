using Content.Shared._RMC14.Botany;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Botany;

public sealed class RMCPlantAnalyzerUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPlantAnalyzerComponent, AfterAutoHandleStateEvent>(OnRefresh);
    }

    private void OnRefresh(Entity<RMCPlantAnalyzerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(ent, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is RMCPlantAnalyzerBui analyzerBui)
            {
                try
                {
                    analyzerBui.Refresh(ent.Comp);
                }
                catch (Exception e)
                {
                    Log.Error($"{nameof(RMCPlantAnalyzerUISystem)}: {e}");
                }
            }
        }
    }
}
