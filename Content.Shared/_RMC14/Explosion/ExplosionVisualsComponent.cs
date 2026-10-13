using Content.Shared._RMC14.Particles;
using Robust.Shared.Prototypes;

// ReSharper disable CheckNamespace
namespace Content.Shared.Explosion.Components;

public sealed partial class ExplosionVisualsComponent
{
    public TimeSpan RMCStartTime;
    public EntProtoId<ExplosionParticlesComponent>? RMCParticlesOverride;
}
