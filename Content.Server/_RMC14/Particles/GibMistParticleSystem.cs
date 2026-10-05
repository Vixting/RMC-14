using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Particles;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Particles;

public sealed class GibMistParticleSystem : EntitySystem
{
    [Dependency] private readonly RMCReagentSystem _reagent = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, BeingGibbedEvent>(OnGibbed);
    }

    private void OnGibbed(Entity<BodyComponent> ent, ref BeingGibbedEvent args)
    {
        var color = Color.Red;
        if (TryComp<BloodstreamComponent>(ent, out var bloodstream) &&
            _reagent.TryIndex(bloodstream.BloodReagent, out var reagent))
        {
            color = reagent.SubstanceColor;
        }

        RaiseNetworkEvent(new GibMistParticleEvent(_transform.GetMapCoordinates(ent), color), Filter.Pvs(ent.Owner));
    }
}
