using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class StunOnExplosionReceivedComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Weak;

    [DataField, AutoNetworkedField]
    public TimeSpan BlindTime = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public FixedPoint2 FlashThreshold = 30;

    [DataField, AutoNetworkedField]
    public TimeSpan BlurTime = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public double HeldThrowRangePerSeverity = 0.2;

    [DataField, AutoNetworkedField]
    public double HeldThrowMaxRange = 14;

    [DataField, AutoNetworkedField]
    public double HeldThrowSpeedPerRange = 2.5;

    [DataField, AutoNetworkedField]
    public double HeldThrowMinSpeed = 4;
}
