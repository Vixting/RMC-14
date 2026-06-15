using Content.Shared._RMC14.Botany;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Botany;

public sealed class GeneEditorUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<GeneEditorComponent, AfterAutoHandleStateEvent>(OnRefresh);
    }

    private void OnRefresh(Entity<GeneEditorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(ent, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is GeneEditorBui editorBui)
            {
                try
                {
                    editorBui.Refresh(ent.Comp);
                }
                catch (Exception e)
                {
                    Log.Error($"{nameof(GeneEditorUISystem)}: {e}");
                }
            }
        }
    }
}
