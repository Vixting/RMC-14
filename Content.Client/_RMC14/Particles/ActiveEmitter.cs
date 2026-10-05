using System.Numerics;
using Content.Shared._RMC14.Particles;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client._RMC14.Particles;

/// <summary>
/// A running particle emitter and its live particle pool.
/// Created by <see cref="ParticleSystem"/>.
/// </summary>
public sealed class ActiveEmitter
{
    public ParticleEffectPrototype Proto = default!;

    /// <summary>Sub-emitter chain depth. Root emitters are 0.</summary>
    public int SubEmitterDepth;

    public MapCoordinates MapCoords;
    public Vector2 SpawnOffset;
    public EntityUid? AttachedEntity;
    public TimeSpan Age;
    public float EmitAccum;
    public bool Exhausted;
    public uint Handle;
    public Color? ColorOverride;
    public float Intensity = 1f;
    public ParticleRuntimeOverrides? Overrides;

    public Vector2 PreviousPosition;
    public Vector2 EmitterVelocity;
    public bool VelocityInitialized;

    public EntityUid? TargetEntity;
    public Vector2? TargetPosition;
    public float EffectiveEmitAngle;

    public readonly List<bool> FiredBursts = new();

    public Texture[] Frames = Array.Empty<Texture>();
    public float[] Delays = Array.Empty<float>();
    public int AnimFrame;
    public float AnimTimer;

    // Pooled particle list — dead slots stay in the list and are reused from FreePool.
    public readonly List<ParticleData> Particles = new();
    public readonly Queue<ParticleData> FreePool = new();

    public bool HasLiveParticles()
    {
        foreach (var p in Particles)
        {
            if (p.Alive)
                return true;
        }
        return false;
    }
}
