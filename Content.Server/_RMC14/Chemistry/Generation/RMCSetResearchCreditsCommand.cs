using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Chemistry.Generation;

[AdminCommand(AdminFlags.Debug)]
public sealed class RMCSetResearchCreditsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystems = default!;

    public string Command => "rmcsetresearchcredits";
    public string Description => "Sets the chemistry research credit total.";
    public string Help => $"{Command} <amount>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || !int.TryParse(args[0], out var amount))
        {
            shell.WriteError("Amount must be a number.");
            return;
        }

        var research = _entitySystems.GetEntitySystem<RMCChemistryResearchSystem>();
        var clamped = research.SetCredits(amount);
        shell.WriteLine($"Research credits set to {clamped}.");
    }
}
