using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Body.Components;

namespace Content.Shared._RMC14.Particles;

public sealed class ParticleBloodColorSystem : EntitySystem
{
    [Dependency] private readonly RMCReagentSystem _reagent = default!;

    public bool TryGetBloodColor(EntityUid uid, out Color color)
    {
        if (TryComp(uid, out ParticleBloodColorComponent? blood))
        {
            color = blood.Color;
            return true;
        }

        if (TryComp(uid, out BloodstreamComponent? bloodstream) &&
            _reagent.TryIndex(bloodstream.BloodReagent, out var reagent))
        {
            color = reagent.SubstanceColor;
            return true;
        }

        color = default;
        return false;
    }
}
