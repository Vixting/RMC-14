using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Particles;

public sealed class RMCParticleEffectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public void PlayPvs(RMCParticleEffectEvent ev)
    {
        if (_net.IsClient)
            return;

        RaiseNetworkEvent(ev, Filter.Pvs(ev.Coords));
    }

    public void PlayPredicted(RMCParticleEffectEvent ev, EntityUid? user)
    {
        if (_net.IsServer)
        {
            var filter = Filter.Pvs(ev.Coords);
            if (user != null)
                filter.RemovePlayerByAttachedEntity(user.Value);

            RaiseNetworkEvent(ev, filter);
            return;
        }

        if (user != null && user == _player.LocalEntity && _timing.IsFirstTimePredicted)
            RaiseLocalEvent(ev);
    }

    public void StopPredicted(EntityUid source, EntityUid? user)
    {
        var ev = new RMCParticleEffectStopEvent(GetNetEntity(source));
        if (_net.IsServer)
        {
            RaiseNetworkEvent(ev, Filter.Pvs(source));
            return;
        }

        if (user != null && user == _player.LocalEntity && _timing.IsFirstTimePredicted)
            RaiseLocalEvent(ev);
    }
}
