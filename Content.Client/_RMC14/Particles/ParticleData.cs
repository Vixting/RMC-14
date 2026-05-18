using System.Numerics;

namespace Content.Client._RMC14.Particles;

/// <summary>
/// A single live particle. Class so it can be pooled in place.
/// </summary>
public sealed class ParticleData
{
    public Vector2 LocalOffset;
    public Vector2 SpawnOrigin;
    public Vector2 Velocity;
    public TimeSpan Age;
    public TimeSpan Lifetime;
    public float SpawnSpeed;
    public float SpawnIntensity;
    public float Rotation;
    public float RotationSpeed;
    public bool Alive;
    public float SizeMultiplier = 1f;
    public Vector2 NoiseOffset;

    public float AgeRatio => Lifetime > TimeSpan.Zero
        ? Math.Clamp((float)(Age.TotalSeconds / Lifetime.TotalSeconds), 0f, 1f)
        : 1f;

    public void Reset()
    {
        LocalOffset = default;
        SpawnOrigin = default;
        Velocity = default;
        Age = TimeSpan.Zero;
        Lifetime = TimeSpan.FromSeconds(1);
        SpawnSpeed = 0f;
        SpawnIntensity = 1f;
        Rotation = 0f;
        RotationSpeed = 0f;
        Alive = false;
        SizeMultiplier = 1f;
        NoiseOffset = default;
    }
}
