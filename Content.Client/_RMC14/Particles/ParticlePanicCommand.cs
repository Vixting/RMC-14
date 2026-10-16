using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Client._RMC14.Particles;

/// <summary>
/// Immediately kills all active particle emitters and their live particles.
/// </summary>
[AnyCommand]
public sealed class ParticlePanicCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "particlepanic";
    public string Description => "Kills all active particle emitters and clears every live particle immediately.";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var particles = _entities.System<ParticleSystem>();
        var count = particles.KillAll();
        shell.WriteLine($"Cleared {count} emitter(s)/particle(s).");
    }
}
