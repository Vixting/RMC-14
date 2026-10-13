using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Particles;

public sealed class RMCParticleEffectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<(RMCParticleEffectEvent Effect, ICommonSession? Excluded)> _pendingEffects = new();
    private readonly List<(NetEntity Source, MapCoordinates Coords)> _pendingStops = new();
    private readonly Dictionary<ICommonSession, RMCParticleEffectBatchEvent> _bySession = new();

    public void PlayPvs(RMCParticleEffectEvent ev)
    {
        if (_net.IsClient)
            return;

        _pendingEffects.Add((ev, null));
    }

    public void PlayPredicted(RMCParticleEffectEvent ev, EntityUid? user)
    {
        if (_net.IsServer)
        {
            ICommonSession? excluded = null;
            if (user != null)
                _player.TryGetSessionByEntity(user.Value, out excluded);

            _pendingEffects.Add((ev, excluded));
            return;
        }

        if (user != null && user == _player.LocalEntity && _timing.IsFirstTimePredicted)
            RaiseLocalEvent(ev);
    }

    public void StopPredicted(EntityUid source, EntityUid? user)
    {
        var netSource = GetNetEntity(source);
        if (_net.IsServer)
        {
            _pendingEffects.RemoveAll(p => p.Effect.Source == netSource);
            _pendingStops.Add((netSource, _transform.GetMapCoordinates(source)));
            return;
        }

        if (user != null && user == _player.LocalEntity && _timing.IsFirstTimePredicted)
            RaiseLocalEvent(new RMCParticleEffectStopEvent(netSource));
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient || _pendingEffects.Count == 0 && _pendingStops.Count == 0)
            return;

        _bySession.Clear();
        foreach (var (source, coords) in _pendingStops)
        {
            foreach (var session in Filter.Pvs(coords).Recipients)
            {
                GetBatch(session).Stops.Add(source);
            }
        }

        foreach (var (effect, excluded) in _pendingEffects)
        {
            foreach (var session in Filter.Pvs(effect.Coords).Recipients)
            {
                if (session == excluded)
                    continue;

                GetBatch(session).Effects.Add(effect);
            }
        }

        _pendingStops.Clear();
        _pendingEffects.Clear();
        foreach (var (session, batch) in _bySession)
        {
            RaiseNetworkEvent(batch, session);
        }

        _bySession.Clear();
    }

    private RMCParticleEffectBatchEvent GetBatch(ICommonSession session)
    {
        if (!_bySession.TryGetValue(session, out var batch))
        {
            batch = new RMCParticleEffectBatchEvent(new List<NetEntity>(), new List<RMCParticleEffectEvent>());
            _bySession[session] = batch;
        }

        return batch;
    }
}
