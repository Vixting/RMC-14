using Content.Shared._RMC14.Botany;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Botany;

public sealed class LysisCentrifugeUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<LysisCentrifugeComponent, AfterAutoHandleStateEvent>(OnRefresh);
    }

    private void OnRefresh(Entity<LysisCentrifugeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(ent, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is LysisCentrifugeBui centBui)
            {
                try
                {
                    centBui.Refresh(ent.Comp);
                }
                catch (Exception e)
                {
                    Log.Error($"{nameof(LysisCentrifugeUISystem)}: {e}");
                }
            }
        }
    }
}
