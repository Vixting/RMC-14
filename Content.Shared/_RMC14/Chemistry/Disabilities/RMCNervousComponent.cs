namespace Content.Shared._RMC14.Chemistry.Disabilities;

/// <summary>
///     cm13 mob_defines.dm NERVOUS: rolls prob(10) roughly every SSmobs tick (2 seconds) to force a
///     stutter, independent of whatever chemical granted it. Persistent until cured - see
///     <see cref="RMCNervousSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(RMCNervousSystem))]
public sealed partial class RMCNervousComponent : Component
{
    [DataField]
    public TimeSpan NextCheck;
}
