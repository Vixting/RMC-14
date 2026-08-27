using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Fluids;

[Serializable, NetSerializable]
public enum RMCFirefighterNozzleMode : byte
{
    Extinguisher,
    MetalFoamLauncher,
    MetalFoamer,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class RMCFirefighterNozzleComponent : Component
{
    [DataField, AutoNetworkedField]
    public RMCFirefighterNozzleMode Mode = RMCFirefighterNozzleMode.Extinguisher;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ExtinguisherCost = FixedPoint2.New(10);

    [DataField, AutoNetworkedField]
    public FixedPoint2 FoamerCost = FixedPoint2.New(10);

    [DataField, AutoNetworkedField]
    public FixedPoint2 LauncherCost = FixedPoint2.New(100);

    [DataField, AutoNetworkedField]
    public EntProtoId FoamPrototype = "RMCAluminiumMetalFoamEffect";

    [DataField, AutoNetworkedField]
    public EntProtoId BallPrototype = "RMCMetalFoamBall";

    [DataField, AutoNetworkedField]
    public int FoamerSpread;

    [DataField, AutoNetworkedField]
    public int LauncherSpread = 12;

    [DataField, AutoNetworkedField]
    public TimeSpan LauncherTravelTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public SoundSpecifier LaunchSound = new SoundPathSpecifier("/Audio/_RMC14/Items/syringeproj.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier FoamSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/bamf.ogg");

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUse;

    [DataField, AutoNetworkedField]
    public TimeSpan FoamerDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan LauncherDelay = TimeSpan.FromSeconds(2);
}
