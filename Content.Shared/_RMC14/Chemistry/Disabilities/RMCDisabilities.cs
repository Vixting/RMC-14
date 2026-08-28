using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

/// <summary>
///     Clears every disability component in this folder from an entity - used by the chem
///     properties that cure disabilities outright rather than granting any specific one
///     (cm13 prop_positive.dm aiding/process, prop_special.dm omnipotent/process).
/// </summary>
public static class RMCDisabilities
{
    public static void ClearAll(IEntityManager entities, EntityUid uid)
    {
        entities.RemoveComponent<RMCNearsightedComponent>(uid);
        entities.RemoveComponent<RMCBlindDisabilityComponent>(uid);
        entities.RemoveComponent<RMCNervousComponent>(uid);
        entities.RemoveComponent<RMCOpiateReceptorDeficiencyComponent>(uid);
        entities.RemoveComponent<RMCDeafDisabilityComponent>(uid);
        entities.RemoveComponent<RMCMuteDisabilityComponent>(uid);
    }
}
