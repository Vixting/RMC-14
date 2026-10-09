using Content.Client._RMC14.Particles;
using Content.Shared._RMC14.Particles;
using Content.Shared._RMC14.Sprite;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Timing;

namespace Content.Client._RMC14.Sprite;

public sealed class RMCSpriteDescentVisualsSystem : EntitySystem
{
    [Dependency] private readonly ParticleEmitterSystem _particleEmitter = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SpriteTreeSystem _spriteTree = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(SpriteTreeSystem));
    }

    public override void FrameUpdate(float frameTime)
    {
        var predictedTicks = Math.Max(0, (int) (_timing.CurTick.Value - _timing.LastRealTick.Value));
        var time = _timing.CurTime - _timing.TickPeriod * predictedTicks;
        var query = EntityQueryEnumerator<RMCSpriteDescentComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var descent, out var sprite))
        {
            var progress = descent.Duration > TimeSpan.Zero
                ? Math.Clamp((float) ((time - descent.StartTime) / descent.Duration), 0f, 1f)
                : 1f;

            var offset = descent.StartOffset * (1f - progress);
            if (sprite.Offset != offset)
            {
                _sprite.SetOffset((uid, sprite), offset);
                _spriteTree.QueueTreeUpdate(uid, sprite);
            }

            if (!TryComp(uid, out ParticleEmitterComponent? particles))
                continue;

            foreach (var emitter in _particleEmitter.GetEmitters(uid))
            {
                emitter.SpawnOffset = particles.SpawnOffset + offset;
            }
        }
    }
}
