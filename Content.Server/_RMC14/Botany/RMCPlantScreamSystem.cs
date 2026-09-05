using Content.Shared._RMC14.Botany;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Plays a plants scream sound if it has the CanScream trait
/// </summary>
public sealed class RMCPlantScreamSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <summary>
    /// Plays <paramref name="plant"/>'s scream sound centered on <paramref name="tray"/>, if it can scream.
    /// </summary>
    public bool DoScream(EntityUid tray, EntityUid? plant)
    {
        if (plant == null || !TryComp(plant, out RMCPlantTraitScreamComponent? scream))
            return false;

        _audio.PlayPvs(scream.ScreamSound, tray);
        return true;
    }
}
