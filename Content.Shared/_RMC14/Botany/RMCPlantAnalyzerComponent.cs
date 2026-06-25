using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCPlantAnalyzerComponent : Component
{
    [DataField]
    public SoundSpecifier ScanSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
}
