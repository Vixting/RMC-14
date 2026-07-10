using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Chemistry.Generation;

[AdminCommand(AdminFlags.Debug)]
public sealed class RMCRerollContractsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystems = default!;

    public string Command => "rmcrerollcontracts";
    public string Description => "Immediately rerolls the chemistry research contracts.";
    public string Help => Command;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var research = _entitySystems.GetEntitySystem<RMCChemistryResearchSystem>();
        research.ForceRerollContracts();
        shell.WriteLine("Research contracts rerolled.");
    }
}
