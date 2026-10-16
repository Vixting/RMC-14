using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Particles;

/// <summary>
/// Blood color used by blood particles, for mobs whose blood isnt a bloodstream reagent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ParticleBloodColorSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class ParticleBloodColorComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Color Color;

    [DataField, AutoNetworkedField]
    public bool SplatterOnHit = true;
}
